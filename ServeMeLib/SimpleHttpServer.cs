namespace ServeMeLib
{
    // MIT License - Copyright (c) 2016 Can Güney Aksakalli
    // https://aksakalli.github.io/2014/02/24/simple-http-server-with-csparp.html
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Net.Sockets;
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
        private string _rootDirectory;

        private Thread _serverThread;

        public object PadLock = new object();

        /// <summary>
        ///     Construct server with suitable port.
        /// </summary>
        /// <param name="path">Directory path to serve.</param>
        public SimpleHttpServer(string path, ServeMe serveMe, int? port = null)
        {
            ServicePointManager.DefaultConnectionLimit = 100;
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
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
            this.Initialize(path, port.Value);
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
            this._serverThread.Abort();
            this._listener.Stop();
            client.Dispose();
        }

        private void Listen()
        {
            this._listener = new HttpListener();
            this._listener.Prefixes.Add("http://*:" + this._port.ToString() + "/");
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
                    this.ServeMe.Log(ex.Message + " " + ex.InnerException?.Message);
                    //Console.WriteLine(ex);
                    if (context?.Response != null)
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        context.Response.OutputStream?.Close();
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
                foreach (string s in content.Split('\n'))
                {
                    if (string.IsNullOrEmpty(s))
                        continue;

                    string[] parts = s.Split(',');
                    if (parts.Length < 2)
                        continue;

                    string from = parts[0].ToLower().Trim();

                    string[] fromParts = from.Split(' ');
                    //todo remove duplicate codes all over here

                    string descriptor = "regex";
                    bool hasDescriptor = fromParts.Length > 1 &&
                        !string.IsNullOrEmpty(fromParts[0].Trim()) &&
                        !string.IsNullOrEmpty(fromParts[1].Trim());
                    if (hasDescriptor)
                    {
                        from = fromParts[1].Trim();
                        descriptor = fromParts[0].Trim();
                    }

                    string pathAndQuery = context.Request.Url.PathAndQuery.ToLower();
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

                    string[] toParts = Regex.Split(parts[1], @"\s{1,}");
                    string to = toParts[0].Trim().ToLower();
                    string saveFile = null;
                    string authType = null;
                    string userName = null;
                    string password = null;
                    if (parts.Length > 4)
                    {
                        string[] saveParts = Regex.Split(parts[4], @"\s{1,}");
                        if (saveParts[0].Trim().ToLower() == "save")
                            saveFile = saveParts[1];
                    }

                    if (toParts.Length > 3)
                        if (toParts[1].Trim().ToLower() == "auth")
                        {
                            authType = toParts[2].ToLower().Trim();
                            userName = toParts[3];
                            password = toParts[4];
                        }

                    filename = to;
                    string expectedMethod = "GET";
                    if (parts.Length > 2)
                        if (!string.IsNullOrEmpty(parts[2].Trim()))
                        {
                            expectedMethod = parts[2].Trim().ToUpper();
                            if (context.Request.HttpMethod.ToLower() != expectedMethod.ToLower())
                            {
                                this.ServeMe.Log($"Found matching setting : {s}", "Returning \'NotFound\' status");
                                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                                context.Response.OutputStream.Close();
                                return;
                            }
                        }

                    if (parts.Length > 3 ||
                        to.StartsWith("{") || to.StartsWith("[") ||
                        to.StartsWith("http://") || to.StartsWith("https://")
                    )
                    {
                        if (parts.Length > 3)
                            responseCode = parts[3].Trim();
                        else
                            responseCode = "200";

                        if (to.StartsWith("http://") || to.StartsWith("https://"))
                        {
                            this.ServeMe.Log($"Found matching setting : {s}", $"Making external call to {to}");

                            HttpRequestMessage request = ToHttpRequestMessage(context.Request, to);

                            if (authType != null)
                            {
                                if (authType == "basic")
                                    request.Headers.Authorization =
                                        new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.Default.GetBytes($"{userName}:{password}")));
                                if (authType == "cookie")
                                {
                                    var clientCookie = new Cookie(userName, password);
                                    clientCookie.Expires = DateTime.Now.AddDays(2);
                                    clientCookie.Domain = request.RequestUri.Host;
                                    clientCookie.Path = "/";
                                    string cook = clientCookie.ToString();
                                    request.Headers.Add("Cookie", cook + ";_ga=GA1.2.2066560216.1541104696");
                                }
                            }

                            ServicePointManager.Expect100Continue = false;
                            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                            //expectedMethod
                            HttpResponseMessage response = this.SendAsync(request, expectedMethod, to);

                            this.ServeMe.Log($"Get {response.StatusCode} response from call to {to} ");

                            context.Response.StatusCode = (int)response.StatusCode;
                            context.Response.ContentType = response.Content.Headers.ContentType.MediaType;
                            string mediaType = response.Content.Headers.ContentType.MediaType.ToLower();
                            if (mediaType.Contains("text") || mediaType.Contains("json"))
                            {
                                string stringResponse = response.Content.ReadAsStringAsync().Result;
                                if (!string.IsNullOrEmpty(saveFile))
                                {
                                    this.ServeMe.Log($"Saving to file {saveFile}...");
                                    lock (this.PadLock)
                                    {
                                        this.ServeMe.WriteAllTextToFile(saveFile, stringResponse);
                                    }
                                }

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

                        if (to.StartsWith("{") || to.StartsWith("["))
                        {
                            this.ServeMe.Log($"Found matching setting : {s}");

                            if (!string.IsNullOrEmpty(responseCode))
                            {
                                int.TryParse(responseCode, out int code);
                                context.Response.StatusCode = code;
                                this.ServeMe.Log($"Returning status code {code}");
                            }

                            string responseData = to.Trim();
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

            if (string.IsNullOrEmpty(filename))
                foreach (string indexFile in this._indexFiles)
                    if (this.ServeMe.FileExists(Path.Combine(this._rootDirectory, indexFile)))
                    {
                        filename = indexFile;
                        break;
                    }

            filename = filename ?? "";
            if (!filename.Contains(":"))
                filename = Path.Combine(this._rootDirectory, filename);
            this.ServeMe.Log($"Working on returning resource {filename}");

            if (this.ServeMe.FileExists(filename))
                try
                {
                    Stream input = new FileStream(filename, FileMode.Open);

                    //Adding permanent http response headers
                    string mime;

                    if (filename.EndsWith(".json"))
                        context.Response.ContentType = "application/json";
                    else if (_mimeTypeMappings.TryGetValue(Path.GetExtension(filename), out mime))
                        context.Response.ContentType = mime;
                    else
                        context.Response.ContentType = "application/octet-stream";

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
            else
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;

            this.ServeMe.Log("Request process completed");

            context.Response.OutputStream.Close();
        }

        private void Initialize(string path, int port)
        {
            this._rootDirectory = path;
            this._port = port;
            this._serverThread = new Thread(this.Listen);
            this._serverThread.Start();
        }

        public HttpResponseMessage SendAsync(
            HttpRequestMessage request,
            string method,
            string remote)
        {
            try
            {
                //todo using task run here now, but it needs to be refactored for performance
                HttpResponseMessage response = Task.Run(() => client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead)).Result;

                response.Headers.Via.Add(new ViaHeaderValue("1.1", "ServeMeProxy", "http"));
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

                this.ServeMe.Log($"{e.GetType().Name} Error while sending {method} request to {request?.RequestUri}", errorMessage);

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
                this.ServeMe.Log($"{e.GetType().Name} Error while sending {method} request to {request?.RequestUri}");

                return new HttpResponseMessage
                {
                    StatusCode = 0,
                    Content = new StringContent(e.Message)
                };
            }
            catch (TaskCanceledException e)
            {
                this.ServeMe.Log($"{e.GetType().Name} Error while sending {method} request to {request?.RequestUri}");

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

                this.ServeMe.Log($"{ex.GetType().Name} Error while sending {method} request to {request?.RequestUri}", message);

                response.Content = new StringContent(message);
                Trace.TraceError("Error:{0}", message);
                return response;
            }
        }

        private static HttpRequestMessage ToHttpRequestMessage(HttpListenerRequest requestInfo, string RewriteToUrl)
        {
            var method = new HttpMethod(requestInfo.HttpMethod);

            var request = new HttpRequestMessage(method, RewriteToUrl);

            //have to explicitly null it to avoid protocol violation
            if (request.Method == HttpMethod.Get || request.Method == HttpMethod.Trace) request.Content = null;

            //now check if the request came from our secure listener then outgoing needs to be secure
            if (request.Headers.Contains("X-Forward-Secure"))
            {
                request.RequestUri = new UriBuilder(request.RequestUri) { Scheme = Uri.UriSchemeHttps, Port = -1 }.Uri;
                request.Headers.Remove("X-Forward-Secure");
            }

            string clientIp = "127.0.0.1";
            request.Headers.Add("X-Forwarded-For", clientIp);
            if (requestInfo.UrlReferrer?.ToString() != null)
                requestInfo.Headers.Add("Referer", requestInfo.UrlReferrer.ToString());

            foreach (string key in requestInfo.Headers)
                if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(requestInfo.Headers[key]))
                {
                    if (request.Headers.Contains(key))
                        request.Headers.Remove(key);
                    request.Headers.Add(key, requestInfo.Headers[key]);
                }

            if (request.Headers.Contains("Host"))
                request.Headers.Remove("Host");
            request.Headers.Add("Host", request.RequestUri.DnsSafeHost);
            return request;
        }
    }
}