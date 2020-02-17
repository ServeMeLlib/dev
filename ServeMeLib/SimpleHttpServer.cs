namespace ServeMeLib
{
    using Microsoft.CSharp;
    using System;
    using System.CodeDom.Compiler;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Net.Sockets;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;

    internal class SimpleHttpServer
    {
        private static readonly IDictionary<string, string> _mimeTypeMappings = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
        {
            #region extension to MIME type list

            { ".asf", "video/x-ms-asf" },
            { ".asx", "video/x-ms-asf" },
            { ".avi", "video/x-msvideo" },
            { ".bin", "application/octet-stream" },
            { ".cco", "application/x-cocoa" },
            { ".crt", "application/x-x509-ca-cert" },
            { ".css", "text/css" },
            { ".deb", "application/octet-stream" },
            { ".der", "application/x-x509-ca-cert" },
            { ".dll", "application/octet-stream" },
            { ".dmg", "application/octet-stream" },
            { ".ear", "application/java-archive" },
            { ".eot", "application/octet-stream" },
            { ".exe", "application/octet-stream" },
            { ".flv", "video/x-flv" },
            { ".gif", "image/gif" },
            { ".hqx", "application/mac-binhex40" },
            { ".htc", "text/x-component" },
            { ".htm", "text/html" },
            { ".html", "text/html" },
            { ".ico", "image/x-icon" },
            { ".img", "application/octet-stream" },
            { ".iso", "application/octet-stream" },
            { ".jar", "application/java-archive" },
            { ".jardiff", "application/x-java-archive-diff" },
            { ".jng", "image/x-jng" },
            { ".jnlp", "application/x-java-jnlp-file" },
            { ".jpeg", "image/jpeg" },
            { ".jpg", "image/jpeg" },
            { ".js", "application/x-javascript" },
            { ".mml", "text/mathml" },
            { ".mng", "video/x-mng" },
            { ".mov", "video/quicktime" },
            { ".mp3", "audio/mpeg" },
            { ".mpeg", "video/mpeg" },
            { ".mpg", "video/mpeg" },
            { ".msi", "application/octet-stream" },
            { ".msm", "application/octet-stream" },
            { ".msp", "application/octet-stream" },
            { ".pdb", "application/x-pilot" },
            { ".pdf", "application/pdf" },
            { ".pem", "application/x-x509-ca-cert" },
            { ".pl", "application/x-perl" },
            { ".pm", "application/x-perl" },
            { ".png", "image/png" },
            { ".prc", "application/x-pilot" },
            { ".ra", "audio/x-realaudio" },
            { ".rar", "application/x-rar-compressed" },
            { ".rpm", "application/x-redhat-package-manager" },
            { ".rss", "text/xml" },
            { ".run", "application/x-makeself" },
            { ".sea", "application/x-sea" },
            { ".shtml", "text/html" },
            { ".sit", "application/x-stuffit" },
            { ".swf", "application/x-shockwave-flash" },
            { ".tcl", "application/x-tcl" },
            { ".tk", "application/x-tcl" },
            { ".txt", "text/plain" },
            { ".war", "application/java-archive" },
            { ".wbmp", "image/vnd.wap.wbmp" },
            { ".wmv", "video/x-ms-wmv" },
            { ".xml", "text/xml" },
            { ".xpi", "application/x-xpinstall" },
            { ".zip", "application/zip" },

            #endregion extension to MIME type list
        };

        private readonly string[] _indexFiles =
        {
            "index.html",
            "index.htm",
            "default.html",
            "default.htm"
        };

        private HttpListener _listener;
        private int _port;
        //private string _rootDirectory;

        private Thread _serverThread;

        public object PadLock = new object();

        /// <summary>
        ///     Construct server with suitable port.
        /// </summary>
        /// <param name="path">Directory path to serve.</param>
        public SimpleHttpServer(ServeMe serveMe, int? port = null)
        {
            ServicePointManager.DefaultConnectionLimit = 100;
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                UseCookies = false
            };

            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                | SecurityProtocolType.Tls11
                | SecurityProtocolType.Tls12
                | SecurityProtocolType.Ssl3;
            client = new HttpClient(handler);

            this.ServeMe = serveMe;
            if (port == null)
            {
                //get an empty port
                var l = new TcpListener(IPAddress.Loopback, 0);
                l.Start();
                port = ((IPEndPoint)l.LocalEndpoint).Port;
                l.Stop();
            }

            this.ServeMe.Log($"Using port {port}");
            this.Initialize(port.Value);
        }

        private ServeMe ServeMe { get; }

        public int Port
        {
            get => this._port;
            private set { }
        }

        private static HttpClient client { set; get; }

        /// <summary>
        ///     Stop server and dispose all functions.
        /// </summary>
        public void Stop()
        {
            this.ServeMe.Log("Stopping server ...");

            this._listener.Stop();
            client.Dispose();
            this._serverThread.Abort();
        }

        private void Listen()
        {
            //https://github.com/arshad115/HttpListenerServer
            /*
            A URI prefix string is composed of a scheme (http or https), a host, an optional port,
            and an optional path. An example of a complete prefix string is "http://www.contoso.com:8080/customerData/".
            Prefixes must end in a forward slash ("/"). The HttpListener object with the prefix that most closely matches
            a requested URI responds to the request. Multiple HttpListener objects cannot add the same prefix; a Win32Exception
            exception is thrown if a HttpListener adds a prefix that is already in use. When a port is specified, the host element
            can be replaced with "*" to indicate that the HttpListener accepts requests sent to the port if the requested URI
            does not match any other prefix. For example, to receive all requests sent to port 8080 when the requested URI is
            not handled by any HttpListener, the prefix is "http://*:8080/". Similarly, to specify that the HttpListener accepts
            all requests sent to a port, replace the host element with the "+" character, "https://+:8080".
            The "*" and "+" characters can be present in prefixes that include paths.
            */
            this._listener = new HttpListener();
            this._listener.Prefixes.Add("http://*:" + this._port.ToString() + "/");
            //this._listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
            //this._listener.Prefixes.Add("http://+:80/");
            this.ServeMe.Log($"About to start listening on port {this._port}");
            this._listener.Start();
            this.ServeMe.Log($"Now listening on port {this._port}");
            while (true)
            {
                HttpListenerContext context = null;
                try
                {
                    context = this._listener.GetContext();
                    this.Process(context);
                }
                catch (Exception ex)
                {
                    try
                    {
                        this.ServeMe.Log(ex.Message + " " + ex.InnerException?.Message);
                        //Console.WriteLine(ex);
                        if (context?.Response != null)
                        {
                            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                            context.Response.OutputStream?.Close();
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        this.ServeMe.Log("FATAL ERROR :" + e.Message + " " + e.InnerException?.Message);
                    }
                }
            }
        }

        private void Process(HttpListenerContext context)
        {
            string filename = context.Request.Url.AbsolutePath;

            this.ServeMe.Log($"Request with {context.Request.HttpMethod} {context.Request.Url} for resource {filename}");

            //Console.WriteLine(filename);
            filename = filename.Substring(1);

            string responseCode = "";
            string content = this.ServeMe.GetSeUpContent();
            if (!string.IsNullOrEmpty(content))
            {
                this.ServeMe.Log("Searching for matching setting ...");
                foreach (string setupLine in content.Split('\n'))
                {
                    if (string.IsNullOrEmpty(setupLine))
                        continue;

                    string s = setupLine.Split(new[] { "***" }, StringSplitOptions.None)[0];
                    var variables = this.ServeMe.GetVariablesFromSettings();

                    if (variables != null)
                    {
                        for (var i = 0; i < variables.Count; i++)
                        {
                            if (!s.StartsWith("app var"))
                            {
                                s = s.Replace("{{" + variables[i][0] + "}}", variables[i][1]);
                            }
                        }
                    }
                    string[] parts = s.Split(',');
                    if (parts.Length < 2)
                        continue;

                    string from = parts[0].ToLower().Trim();

                    from = replaceTokensForTo(from, context);

                    string[] fromParts = from.Split(' ');
                    //todo remove duplicate codes all over here

                    string descriptor = "regex";
                    bool hasDescriptor = fromParts.Length > 1 &&
                        !string.IsNullOrEmpty(fromParts[0].Trim()) &&
                        !string.IsNullOrEmpty(fromParts[1].Trim());
                    if (hasDescriptor)
                    {
                        this.ServeMe.Log("Request settings has a descriptor");
                        from = fromParts[1].Trim().ToLower();
                        descriptor = fromParts[0].Trim();
                        this.ServeMe.Log($"Descriptor : {descriptor} from {from}");
                    }

                    string pathAndQuery = context.Request.Url.PathAndQuery.ToLower();
                    this.ServeMe.Log($"Matching descriptor  {descriptor} with request path {pathAndQuery} ...");
                    switch (descriptor)
                    {
                        case "equalto":
                            {
                                if (from != pathAndQuery)
                                    continue;
                                break;
                            }
                        case "!equalto":
                            {
                                if (from == pathAndQuery)
                                    continue;
                                break;
                            }
                        case "contains":
                            {
                                if (!pathAndQuery.Contains(from))
                                    continue;
                                break;
                            }
                        case "!contains":
                            {
                                if (pathAndQuery.Contains(from))
                                    continue;
                                break;
                            }
                        case "startswith":
                            {
                                if (!pathAndQuery.StartsWith(from))
                                    continue;
                                break;
                            }
                        case "!startswith":
                            {
                                if (pathAndQuery.StartsWith(from))
                                    continue;
                                break;
                            }
                        case "endswith":
                            {
                                if (!pathAndQuery.EndsWith(from))
                                    continue;
                                break;
                            }
                        case "!endswith":
                            {
                                if (pathAndQuery.EndsWith(from))
                                    continue;
                                break;
                            }
                        case "regex":
                            {
                                if (!new Regex(from).Match(pathAndQuery.Trim()).Success)
                                    continue;
                                break;
                            }
                        case "!regex":
                            {
                                if (new Regex(from).Match(pathAndQuery.Trim()).Success)
                                    continue;
                                break;
                            }
                        default:
                            continue;
                    }

                    string[] toParts = Regex.Split(parts[1].Trim(), @"\s{1,}");
                    string to = toParts[0].Trim();
                    to = replaceTokensForTo(to, context);

                    string[] toPossiblePartsPart = parts[1].Trim().Split(new[] { ' ' }, 2);
                    bool expectedJson = false;
                    string toFirstPart = toPossiblePartsPart[0].Trim().ToLower();
                    if (toPossiblePartsPart.Length > 1)
                    {
                        this.ServeMe.Log($"Response is expected to be {toFirstPart}");
                        if (toFirstPart == "json")
                        {
                            expectedJson = true;
                            to = toPossiblePartsPart[1].Trim();
                            toParts = Regex.Split(toPossiblePartsPart[1].Trim(), @"\s{1,}");
                        }
                        else if (toFirstPart == "assembly")
                        {
                            expectedJson = true;
                            to = toPossiblePartsPart[1].Trim();
                            toParts = Regex.Split(toPossiblePartsPart[1].Trim(), @"\s{1,}");

                            if (toParts.Length < 3)
                                throw new Exception($"Incomplete assemply instruction from input {toPossiblePartsPart[1]} : I was expecting something like 'assembly file:///D:/ServeMe.Tests/bin/Debug/ServeMe.Tests.DLL ServeMe.Tests.when_serve_me_runs DoSomething w,get'");

                            object result = toParts.Length > 3 ? InvokeMethod(toParts[0].Trim(), toParts[1].Trim(), toParts[2].Trim(), toParts[3].Trim()) : InvokeMethod(toParts[0].Trim(), toParts[1].Trim(), toParts[2].Trim());

                            to = result.ToString();
                            toParts[1] = to;
                        }
                        else if (toFirstPart == "sourcecode")
                        {
                            expectedJson = true;
                            to = toPossiblePartsPart[1].Trim();
                            toParts = Regex.Split(toPossiblePartsPart[1].Trim(), @"\s{1,}");

                            if (toParts.Length < 2)
                                throw new Exception($"Incomplete assemply instruction from input {toPossiblePartsPart[1]} : I was expecting something like 'sourcecode csharp xyz.txt w,get'");
                            string lang = toParts[0].Trim();
                            string filen = toParts[1].Trim();
                            if (lang.ToLower() != "csharp")
                                throw new Exception("Only CSharp script is supported at this time");

                            object result = null;

                            string source = File.ReadAllText(filen);
                            if (!string.IsNullOrEmpty(source))
                            {
                                if (lang.ToLower() == "csharp")
                                {
                                    result = toParts.Length > 2 ? Execute(source, toParts[2].Trim()) : Execute(source);
                                    to = result.ToString();
                                }
                                else if (lang.ToLower() == "javascript")
                                {
                                    //ProcessStartInfo psi = new ProcessStartInfo();
                                    //psi.UseShellExecute = false;
                                    //psi.CreateNoWindow = true;
                                    //psi.FileName = @"node.exe";
                                    //psi.Arguments = @"-any -arguments -go Here";
                                    //using (var process =System.Diagnostics.Process.Start(psi))
                                    //{
                                    //    // Do something with process if you want.
                                    //}
                                }
                            }
                            else
                            {
                                throw new Exception("Empty source code found");
                            }

                            toParts[1] = to;
                        }
                        else if (toFirstPart == "atl" || toFirstPart == "appendtolink" || toFirstPart == "appendmatchedpathandquerytolink")
                        {
                            to = toPossiblePartsPart[1].Trim().TrimEnd('\\').TrimEnd('/') + context.Request.Url.PathAndQuery;

                            to = replaceTokensForTo(to, context);

                            toParts = Regex.Split(toPossiblePartsPart[1].Trim(), @"\s{1,}");
                        }
                        else if (toFirstPart == "appendmatchedpathtolink")
                        {
                            to = toPossiblePartsPart[1].Trim().TrimEnd('\\').TrimEnd('/') + context.Request.Url.PathAndQuery.Split('#')[0].Split('?')[0];
                            to = replaceTokensForTo(to, context);
                            toParts = Regex.Split(toPossiblePartsPart[1].Trim(), @"\s{1,}");
                        }
                        else if (toFirstPart == "appendmatchedquerytolink")
                        {
                            string[] pathAndQueryParts = context.Request.Url.PathAndQuery.Split('#')[0].Split('?');
                            string append = pathAndQueryParts.Length > 1 ? pathAndQueryParts[1] : "";
                            to = toPossiblePartsPart[1].Trim().TrimEnd('\\').TrimEnd('/') + append;
                            to = replaceTokensForTo(to, context);
                            toParts = Regex.Split(toPossiblePartsPart[1].Trim(), @"\s{1,}");
                        }
                        else
                        {
                            this.ServeMe.Log($"Could not match expected request type of {toFirstPart} in this branch");
                        }
                    }

                    string saveFile = null;
                    string authType = null;
                    string userName = null;
                    string password = null;
                    bool saveAsServed = false;
                    string find = null;
                    string replace = null;
                    if (parts.Length > 4)
                    {
                        string[] saveParts = Regex.Split(parts[4].Trim(), @"\s{1,}");
                        if (saveParts.Length > 1)
                        {
                            if (saveParts[0].Trim().ToLower() == "save")
                                saveFile = saveParts[1].Trim();
                            if (saveParts[0].Trim().ToLower() == "saveasserved")
                            {
                                if (context.Request.Url.IsFile)
                                    saveFile = Path.GetFileName(context.Request.Url.LocalPath);
                                else
                                    saveFile = saveParts[1].Trim();
                            }
                        }

                        if (saveParts.Length > 3)
                        {
                            find = saveParts[2].Trim();
                            replace = saveParts[3].Trim();
                        }
                    }

                    if (toParts.Length > 3)
                        if (toParts[1].Trim().ToLower() == "auth")
                        {
                            authType = toParts[2].ToLower().Trim();
                            userName = toParts[3].Trim();
                            password = toParts.Length > 4 ? toParts[4].Trim() : "";

                            this.ServeMe.Log($"Auth is configured for this request with auth type {authType}, user anme {userName} and password xxxxxxxxxx");
                        }

                    filename = to;
                    string expectedMethodFrom = "GET";
                    string methodTo = "GET";
                    string filter = null;
                    bool isJsonP = false;
                    if (parts.Length > 2)
                        if (!string.IsNullOrEmpty(parts[2].Trim()))
                        {
                            expectedMethodFrom = parts[2].Trim().ToUpper();
                            expectedMethodFrom = replaceTokensForTo(expectedMethodFrom, context);
                            if (expectedMethodFrom.Contains("|"))
                            {
                                string[] methodParts = expectedMethodFrom.Split('|');
                                expectedMethodFrom = string.IsNullOrEmpty(methodParts[0]) ? "GET" : methodParts[0].Trim();
                                filter = methodParts[1].Trim();
                            }
                            if (expectedMethodFrom.Contains("-"))
                            {
                                string[] methodParts = expectedMethodFrom.Split('-');
                                expectedMethodFrom = string.IsNullOrEmpty(methodParts[0]) ? "GET" : methodParts[0].Trim();
                                methodTo = methodParts[1].Trim();
                            }
                            if (expectedMethodFrom.ToLower().Trim() == "getjsonp")
                            {
                                expectedMethodFrom = "GET";
                                isJsonP = true;
                            }
                            if (context.Request.HttpMethod.ToLower() != expectedMethodFrom.ToLower())
                            {
                                this.ServeMe.Log($"Found matching setting : {s}", "Returning \'NotFound\' status");
                                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                                context.Response.OutputStream.Close();
                                return;
                            }
                        }

                    this.ServeMe.Log($"Expected request method is {expectedMethodFrom}");
                    this.ServeMe.Log($"Expected send method is {methodTo}");
                    if (parts.Length > 3 ||
                        expectedJson /*to.StartsWith("{") || to.StartsWith("[")*/ ||
                        to.StartsWith("http://") || to.StartsWith("https://")
                    )
                    {
                        to = replaceTokensForTo(to, context);

                        if (parts.Length > 3)
                            responseCode = parts[3].Trim();
                        else
                            responseCode = "200";

                        if (!expectedJson && (to.StartsWith("http://") || to.StartsWith("https://")))
                        {
                            this.ServeMe.Log($"Found matching setting : {s}", $"Making external call to {to}");

                            HttpRequestMessage request = ToHttpRequestMessage(context.Request, to, methodTo);

                            if (authType != null)
                            {
                                if (authType == "basic")
                                    request.Headers.Authorization =
                                        new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.Default.GetBytes($"{userName}:{password}")));
                                if (authType == "cookie")
                                    request.Headers.Add("Cookie", userName);
                            }

                            ServicePointManager.Expect100Continue = false;
                            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                                                                   | SecurityProtocolType.Tls11
                                                                   | SecurityProtocolType.Tls12
                                                                   | SecurityProtocolType.Ssl3;
                            //expectedMethod
                            HttpResponseMessage response = this.Send(request, this.ServeMe.Log);

                            this.ServeMe.Log($"Get {response.StatusCode} response from call to {to} ");

                            context.Response.StatusCode = (int)response.StatusCode;
                            if (response.Content.Headers.ContentType != null)
                                context.Response.ContentType = response.Content.Headers.ContentType.MediaType;
                            string mediaType = response.Content.Headers.ContentType?.MediaType?.ToLower() ?? "";
                            if (mediaType.Contains("text") || mediaType.Contains("json") || mediaType.Contains("javascript"))
                            {
                                string stringResponse = response.Content.ReadAsStringAsync().Result;

                                if (!string.IsNullOrEmpty(filter))
                                {
                                    filter = filter.ToLower().Trim();
                                    //power of filters
                                    if (filter == "tolower")
                                    {
                                        stringResponse = stringResponse.ToLower();
                                    }
                                    if (filter == "jsonp")
                                    {
                                        isJsonP = true;
                                    }
                                }

                                if (isJsonP)
                                {
                                    this.ServeMe.Log($"jsonp response requested");
                                    var callback = context.Request.Url.ToString().Split(new char[] { '?', '&' }).Where(x => x.ToLower().StartsWith("callback=")).Select(x => x.Replace("callback=", "")).FirstOrDefault();
                                    if (!string.IsNullOrEmpty(callback))
                                    {
                                        stringResponse = callback + "(" + stringResponse + ")";
                                        this.ServeMe.Log($"jsonp formed!");
                                    }
                                    else
                                    {
                                        this.ServeMe.Log($"No jsonp callback could be inferred from the request");
                                    }
                                }

                                stringResponse = this.ServeMe.ExecuteTemplate(stringResponse);

                                if (!string.IsNullOrEmpty(saveFile))
                                {
                                    this.ServeMe.Log($"Saving to file {saveFile}...");

                                    if (!string.IsNullOrEmpty(find) && !string.IsNullOrEmpty(replace))
                                        stringResponse = stringResponse.Replace(find, replace);
                                    lock (this.PadLock)
                                    {
                                        this.ServeMe.WriteAllTextToFile(saveFile, stringResponse);
                                    }
                                }

                                this.ServeMe.Log("Writing response to stream");
                                new MemoryStream(Encoding.Default.GetBytes(stringResponse)).WriteTo(context.Response.OutputStream);
                            }
                            else
                            {
                                HttpContent httpContent = response.Content;
                                if (!string.IsNullOrEmpty(saveFile))
                                    using (FileStream newFile = File.Create(saveFile))
                                    {
                                        this.ServeMe.Log($"Saving to file {saveFile}...");
                                        lock (this.PadLock)
                                        {
                                            bool result = Task.Run(
                                                async () =>
                                                {
                                                    Stream stream = await httpContent.ReadAsStreamAsync();
                                                    await stream.CopyToAsync(newFile);
                                                    return true;
                                                }).Result;
                                        }
                                    }
                            }

                            context.Response.OutputStream.Close();
                            return;
                        }

                        if (expectedJson || to.StartsWith("{") || to.StartsWith("["))
                        {
                            this.ServeMe.Log($"Found matching setting : {s}");

                            if (!string.IsNullOrEmpty(responseCode))
                            {
                                int.TryParse(responseCode, out int code);
                                context.Response.StatusCode = code;
                                this.ServeMe.Log($"Returning status code {code}");
                            }

                            string responseData = to.Trim();
                            responseData = this.ServeMe.ExecuteTemplate(responseData);
                            if (!string.IsNullOrEmpty(responseData))
                            {
                                context.Response.ContentType = "application/json";
                                this.ServeMe.Log($"Returning json {responseData}");
                                new MemoryStream(Encoding.Default.GetBytes(responseData)).WriteTo(context.Response.OutputStream);
                            }

                            context.Response.OutputStream.Close();
                            return;
                        }
                    }

                    break;
                }
            }

            if (filename.Contains("{{") && filename.Contains("}}"))
            {
                filename = replaceTokensForTo(filename, context);
            }

            if (string.IsNullOrEmpty(filename))
                foreach (string indexFile in this._indexFiles)
                    if (this.ServeMe.FileExists(Path.Combine(ServeMe.CurrentPath, indexFile)))
                    {
                        filename = indexFile;
                        break;
                    }

            filename = filename ?? "";
            if (!filename.Contains(":"))
                filename = Path.Combine(ServeMe.CurrentPath, filename.TrimStart('\\').TrimStart('/'));
            this.ServeMe.Log($"Working on returning resource {filename}");

            if (this.ServeMe.FileExists(filename))
            {
                string mime;

                if (filename.EndsWith(".json"))
                    context.Response.ContentType = "application/json";
                else if (_mimeTypeMappings.TryGetValue(Path.GetExtension(filename), out mime))
                    context.Response.ContentType = mime;
                else
                    context.Response.ContentType = "application/octet-stream";

                if (filename.EndsWith(".htm") ||
                    filename.EndsWith(".html") ||
                    filename.EndsWith(".js") ||
                    filename.EndsWith(".css") ||
                    filename.EndsWith(".txt") ||
                    filename.EndsWith(".json"))
                {
                    var stringResponse = System.IO.File.ReadAllText(filename);
                    stringResponse = this.ServeMe.ExecuteTemplate(stringResponse);
                    new MemoryStream(Encoding.Default.GetBytes(stringResponse)).WriteTo(context.Response.OutputStream);
                    context.Response.OutputStream.Close();
                }
                else
                {
                    try
                    {
                        //Adding permanent http response headers

                        Stream input = new FileStream(filename, FileMode.Open);

                        context.Response.ContentLength64 = input.Length;
                        context.Response.AddHeader("Date", DateTime.Now.ToString("r"));
                        context.Response.AddHeader("Last-Modified", File.GetLastWriteTime(filename).ToString("r"));

                        var buffer = new byte[1024 * 16];
                        int nbytes;
                        while ((nbytes = input.Read(buffer, 0, buffer.Length)) > 0)
                            context.Response.OutputStream.Write(buffer, 0, nbytes);
                        input.Close();

                        context.Response.StatusCode = !string.IsNullOrEmpty(responseCode) && int.TryParse(responseCode, out int code) ? code : (int)HttpStatusCode.OK;
                        context.Response.OutputStream.Flush();
                    }
                    catch (Exception ex)
                    {
                        this.ServeMe.Log($"Error occured while returning resource {filename} : {ex.Message} {ex.InnerException?.Message}");
                        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    }
                }
            }
            else
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;

            this.ServeMe.Log("Request process completed");

            context.Response.OutputStream.Close();
        }

        private static string replaceTokensForTo(string to, HttpListenerContext context)
        {
            List<string> tokensPasts = extractTokens(context);
            for (var i = 0; i < tokensPasts.Count; i++)
            {
                to = replaceTokensForToInt(tokensPasts, to, i);
            }

            to = to.Replace("{{query}}", context.Request.Url.Query.Replace("?", ""));
            to = to.Replace("{{file}}", context.Request.Url.Segments.Last().Replace("/", "").Replace("?", ""));
            to = to.Replace("{{root}}", context.Request.Url.Scheme + "://" + context.Request.Url.Authority);
            to = to.Replace("{{port}}", context.Request.Url.Port.ToString());
            to = to.Replace("{{scheme}}", context.Request.Url.Scheme.ToString());
            to = to.Replace("{{domain}}", context.Request.Url.Authority.ToString());
            to = to.Replace("{{host}}", context.Request.Url.Host.ToString());
            to = to.Replace("{{pathandquery}}", context.Request.Url.PathAndQuery.ToString());
            to = to.Replace("{{path}}", context.Request.Url.AbsolutePath.ToString());
            to = to.Replace("{{extension}}", Path.GetExtension(context.Request.Url.ToString()).Split('?')[0]);
            to = to.Replace("{{noscheme}}", context.Request.Url.ToString().Replace(context.Request.Url.Scheme + "://", ""));
            to = to.Replace("{{httpurl}}", context.Request.Url.ToString().Replace(context.Request.Url.Scheme + "://", "http://"));
            to = to.Replace("{{httpsurl}}", context.Request.Url.ToString().Replace(context.Request.Url.Scheme + "://", "https://"));

            return to;
        }

        private static string replaceTokensForToInt(List<string> tokensPasts, string to, int i)
        {
            if (tokensPasts.Count > i)
            {
                to = to.Replace("{{" + i + "}}", tokensPasts[i]);
            }

            return to;
        }

        private static List<string> extractTokens(HttpListenerContext context)
        {
            var tokensPasts = new List<string>();
            tokensPasts.Add(context.Request.Url.Scheme);
            tokensPasts.Add(context.Request.Url.Host);
            tokensPasts.Add(context.Request.Url.Port.ToString());
            tokensPasts.AddRange(context.Request.Url.AbsolutePath.Split('/').Where(x => !string.IsNullOrEmpty(x)).ToList());
            tokensPasts.Add(context.Request.Url.Query.Split('#')[0].Replace("?", ""));
            if (context.Request.Url.Query.Split('#').Length > 1)
            {
                tokensPasts.Add(context.Request.Url.Query.Split('#')[1].Replace("?", ""));
            }

            return tokensPasts;
        }

        private void Initialize(int port)
        {
            this._port = port;
            this._serverThread = new Thread(this.Listen);
            this._serverThread.Start();
        }

        public HttpResponseMessage Send(
            HttpRequestMessage request,
            Func<string[], bool> Log,
            Action<Exception> onError = null)
        {
            try
            {
                //todo using task run here now, but it needs to be refactored for performance
                request.Headers.AcceptEncoding.Clear();
                request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
                request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
                HttpResponseMessage response = Task.Run(() => client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead)).Result;

                response.Headers.Via.Add(new ViaHeaderValue("1.2", "ServeMeProxy", "http"));
                //same again clear out due to protocol violation
                if (request.Method == HttpMethod.Head)
                    response.Content = null;

                return response;
            }
            catch (HttpRequestException e)
            {
                string errorMessage = e.Message;
                if (e.InnerException != null)
                    errorMessage += " - " + e.InnerException.Message;

                Log(new[] { $"{e.GetType().Name} Error while sending {request.Method} request to {request?.RequestUri}", errorMessage });
                onError?.Invoke(e);
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadGateway,
                    Content = new StringContent(errorMessage)
                };
            }
            catch (PlatformNotSupportedException e)
            {
                // For instance, on some OSes, .NET Core doesn't yet
                // support ServerCertificateCustomValidationCallback
                Log(new[] { $"{e.GetType().Name} Error while sending {request.Method} request to {request?.RequestUri}" });
                onError?.Invoke(e);
                return new HttpResponseMessage
                {
                    StatusCode = 0,
                    Content = new StringContent(e.Message)
                };
            }
            catch (TaskCanceledException e)
            {
                Log(new[] { $"{e.GetType().Name} Error while sending {request.Method} request to {request?.RequestUri}" });
                onError?.Invoke(e);
                return new HttpResponseMessage
                {
                    StatusCode = 0,
                    Content = new StringContent(e.Message + " The endpoint might be unreachable.")
                };
            }
            catch (Exception ex)
            {
                var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
                //context.Response.StatusCode = (int)response.StatusCode;
                string message = ex.Message;
                if (ex.InnerException != null)
                    message += ':' + ex.InnerException.Message;

                Log(new[] { $"{ex.GetType().Name} Error while sending {request.Method} request to {request?.RequestUri}", message });

                response.Content = new StringContent(message);
                Trace.TraceError("Error:{0}", message);
                onError?.Invoke(ex);
                return response;
            }
        }

        private static HttpRequestMessage ToHttpRequestMessage(HttpListenerRequest requestInfo, string RewriteToUrl, string HttpMethodStr = null)
        {
            var method = new HttpMethod(HttpMethodStr ?? requestInfo.HttpMethod);

            var request = new HttpRequestMessage(method, RewriteToUrl);

            //have to explicitly null it to avoid protocol violation
            if (request.Method == HttpMethod.Get || request.Method == HttpMethod.Trace || request.Method == HttpMethod.Head || request.Method == HttpMethod.Delete)
                request.Content = null;
            else
                using (Stream receiveStream = requestInfo.InputStream)
                {
                    using (var readStream = new StreamReader(receiveStream))
                    {
                        string documentContents = readStream.ReadToEnd();
                        Console.WriteLine(documentContents);

                        request.Content = new StringContent(
                            documentContents,
                            Encoding.UTF8,
                            "application/json");
                    }
                }

            //now check if the request came from our secure listener then outgoing needs to be secure
            if (request.Headers.Contains("X-Forward-Secure"))
            {
                request.RequestUri = new UriBuilder(request.RequestUri) { Scheme = Uri.UriSchemeHttps, Port = -1 }.Uri;
                request.Headers.Remove("X-Forward-Secure");
            }

            string clientIp = "127.0.0.1";
            request.Headers.Add("X-Forwarded-For", clientIp);
            //if (requestInfo.UrlReferrer?.ToString() != null)
            //    requestInfo.Headers.Add("Referer", requestInfo.UrlReferrer.ToString());

            foreach (string key in requestInfo.Headers)
                if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(requestInfo.Headers[key]))
                    if (request.Headers.TryAddWithoutValidation(key, requestInfo.Headers[key]))
                    {
                    }

            if (request.Headers.Contains("Host"))
                request.Headers.Remove("Host");

            request.Headers.Add("Host", request.RequestUri.DnsSafeHost);
            if (!request.Headers.Contains("UserAgent"))
                request.Headers.Add("UserAgent", "Mozilla/5.0 (Windows; U; Windows NT 5.1; en-US; rv:1.9.0.1) Gecko/2008070208 Firefox/3.0.1");
            return request;
        }

        /*
        Object [] args = {1, "2", 3.0};
       Object Result = DynaInvoke("c:\FullPathToDll.DLL",
                "ClassName", "MethodName", args);

        */

        public static object InvokeMethod(
            string assemblyFileNameWithFullPath,
            string className,
            string methodName,
            params object[] args)
        {
            args = args ?? new object[] { };
            // load the assemly
            Assembly assembly = Assembly.LoadFrom(assemblyFileNameWithFullPath);

            // Walk through each type in the assembly looking for our class
            foreach (Type type in assembly.GetTypes())
                if (type.IsClass)
                    if (type.FullName.EndsWith(className))
                    {
                        // Dynamically Invoke the method
                        object Result = type.InvokeMember(methodName, BindingFlags.Default | BindingFlags.InvokeMethod, null, Activator.CreateInstance(type), args);
                        return Result;
                    }

            throw new Exception($"could not invoke method {methodName} in assembly {assemblyFileNameWithFullPath} in class {className}");
        }

        private static int NumberOfMatches(string orig, string find)
        {
            string s2 = orig.Replace(find, "");
            return (orig.Length - s2.Length) / find.Length;
        }

        /// <summary>
        ///     This method can return System.Exception or  CompilerErrorCollection if compillation fails
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>
        public static object Execute(string body, params object[] args)
        {
            try
            {
                if (args != null && args.Length == 0)
                    args = null;

                if (!body.Contains("\n") && !body.Contains("\r") && !body.StartsWith("return "))
                    body = "return " + body;

                string methodName = "Method_" + Guid.NewGuid().ToString().Replace("-", "");
                string methodNameHost = "Method_" + Guid.NewGuid().ToString().Replace("-", "");
                string NameSpaceName = "Method_" + Guid.NewGuid().ToString().Replace("-", "");
                string className = "Method_" + Guid.NewGuid().ToString().Replace("-", "");

                string returnStatement = @"";
                //todo ver ineficient
                if (NumberOfMatches(body, "return;") > 0 || NumberOfMatches(body, "return ") == 0)
                    returnStatement = @"return """" ;";

                var providerOptions = new Dictionary<string, string>();
                var provider = new CSharpCodeProvider(providerOptions);

                var compilerParams = new CompilerParameters
                {
                    GenerateInMemory = true,
                    GenerateExecutable = false,
                    // Set compiler argument to optimize output.
                    CompilerOptions = "/optimize",
                    // Set a temporary files collection.
                    // The TempFileCollection stores the temporary files
                    // generated during a build in the current directory,
                    // and does not delete them after compilation.
                    //========todo
                    //TempFiles = new TempFileCollection(".", true),
                    // Generate debug information.
                    IncludeDebugInformation = true
                };

                string namespaces = "";

                var dictionary = new Dictionary<string, string>();

                foreach (AssemblyName assemblyName in Assembly.GetExecutingAssembly().GetReferencedAssemblies())
                {
                    Assembly assembly = Assembly.Load(assemblyName);
                    foreach (Type type in assembly.GetTypes().Where(x => x.Namespace != null))
                    {
                        if (dictionary.ContainsKey(type.Namespace))
                            continue;
                        namespaces += "using " + type.Namespace + ";";
                        dictionary.Add(type.Namespace, type.Namespace);
                        compilerParams.ReferencedAssemblies.Add(type.Module.FullyQualifiedName);
                    }
                }

                string function = args == null
                    ? @"

                        public object " + methodName + @"()
                        {
                            try{" + body + @";" + returnStatement + @"}catch(Exception e){return e;}
                        }
                      "
                    : body;

                string source =
                    @"namespace  " + NameSpaceName + @"
                {
                    " + namespaces + @"
                    public class  " + className + @"
                    {
                        public object " + methodNameHost + @"(params object[] args)
                        {
                            return typeof(" + className + @").InvokeMember(""" + methodName + @""",BindingFlags.Default | BindingFlags.InvokeMethod,null, Activator.CreateInstance(typeof(" + className + @")), args);
                        }
                      " + function + @"
                    }
                }
            ";

                CompilerResults results = provider.CompileAssemblyFromSource(compilerParams, source);

                if (results.Errors.Count != 0)
                {
                    string error = "";
                    foreach (CompilerError resultsError in results?.Errors)
                        error += resultsError?.ErrorText + " - " + resultsError + Environment.NewLine;

                    return error;
                }

                object o = results.CompiledAssembly.CreateInstance(NameSpaceName + "." + className);
                MethodInfo mi = o.GetType().GetMethod(methodNameHost);
                object result = mi.Invoke(o, new object[] { args });
                return result;
            }
            catch (Exception e)
            {
                return e;
            }
        }
    }
}