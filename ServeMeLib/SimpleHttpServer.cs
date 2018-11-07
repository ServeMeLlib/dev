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
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;

    class SimpleHttpServer
    {
        static readonly IDictionary<string, string> _mimeTypeMappings = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
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

        readonly string[] _indexFiles =
        {
            "index.html",
            "index.htm",
            "default.html",
            "default.htm"
        };

        HttpListener _listener;
        int _port;
        string _rootDirectory;

        Thread _serverThread;

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

            this.ServerCsv = serveMe.ServerCsv;
            if (port == null)
            {
                //get an empty port
                var l = new TcpListener(IPAddress.Loopback, 0);
                l.Start();
                port = ((IPEndPoint)l.LocalEndpoint).Port;
                l.Stop();
            }

            this.Initialize(path, port.Value);
        }

        string ServerCsv { get; }

        public int Port
        {
            get => this._port;
            private set { }
        }

        static HttpClient client { set; get; }

        /// <summary>
        ///     Stop server and dispose all functions.
        /// </summary>
        public void Stop()
        {
            this._serverThread.Abort();
            this._listener.Stop();
            client.Dispose();
        }

        void Listen()
        {
            this._listener = new HttpListener();
            this._listener.Prefixes.Add("http://*:" + this._port.ToString() + "/");
            this._listener.Start();
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
                    Console.WriteLine(ex);
                    if (context?.Response != null)
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        context.Response.OutputStream?.Close();
                    }
                }
            }
        }

        void Process(HttpListenerContext context)
        {
            string filename = context.Request.Url.AbsolutePath;
            //Console.WriteLine(filename);
            filename = filename.Substring(1);
            string currentPath = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location ?? Directory.GetCurrentDirectory());
            string loc = currentPath + "\\server.csv";
            string responseCode = "";
            if (!string.IsNullOrEmpty(this.ServerCsv) || File.Exists(loc))
            {
                string content = this.ServerCsv ?? File.ReadAllText(loc);

                foreach (string s in content.Split('\n'))
                {
                    if (string.IsNullOrEmpty(s))
                        continue;

                    string[] parts = s.ToLower().Split(',');
                    if (parts.Length < 2)
                        continue;

                    string from = parts[0].Trim();

                    string[] fromParts = from.Split(' ');
                    //todo remove duplicate codes all over here
                    if (fromParts.Length > 1 && !string.IsNullOrEmpty(fromParts[0]) && !string.IsNullOrEmpty(fromParts[1]))
                    {
                        from = fromParts[1].Trim();
                        if ("exactly" == fromParts[0].Trim())
                        {
                            if (from != context.Request.Url.PathAndQuery.ToLower())
                                continue;
                        }
                        else
                        {
                            if (!new Regex(from).Match(context.Request.Url.PathAndQuery.ToLower().Trim()).Success)
                                continue;
                        }
                    }
                    else
                    {
                        if (!new Regex(from).Match(context.Request.Url.PathAndQuery.ToLower().Trim()).Success)
                            continue;
                    }

                    string to = parts[1].Trim();
                    filename = to;
                    string expectedMethod = "GET";
                    if (parts.Length > 2)
                        if (!string.IsNullOrEmpty(parts[2].Trim()))
                        {
                            expectedMethod = parts[2].Trim().ToUpper();
                            if (context.Request.HttpMethod.ToLower() != expectedMethod.ToLower())
                            {
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
                        responseCode = parts[3].Trim();

                        if (to.StartsWith("http://") || to.StartsWith("https://"))
                        {
                            //expectedMethod
                            HttpResponseMessage response = SendAsync(ToHttpRequestMessage(context.Request, to), expectedMethod, to);
                            context.Response.StatusCode = (int)response.StatusCode;
                            string stringResponse = response.Content.ReadAsStringAsync().Result;
                            context.Response.ContentType = response.Content.Headers.ContentType.MediaType;
                            new MemoryStream(Encoding.Default.GetBytes(stringResponse)).WriteTo(context.Response.OutputStream);

                            context.Response.OutputStream.Close();
                            return;
                        }

                        if (to.StartsWith("{") || to.StartsWith("["))
                        {
                            if (!string.IsNullOrEmpty(responseCode))
                            {
                                int.TryParse(responseCode, out int code);
                                context.Response.StatusCode = code;
                            }

                            string responseData = to.Trim();
                            if (!string.IsNullOrEmpty(responseData))
                            {
                                context.Response.ContentType = "application/json";
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
                    if (File.Exists(Path.Combine(this._rootDirectory, indexFile)))
                    {
                        filename = indexFile;
                        break;
                    }

            filename = filename ?? "";
            if (!filename.Contains(":"))
                filename = Path.Combine(this._rootDirectory, filename);

            if (File.Exists(filename))
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
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                }
            else
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;

            context.Response.OutputStream.Close();
        }

        void Initialize(string path, int port)
        {
            this._rootDirectory = path;
            this._port = port;
            this._serverThread = new Thread(this.Listen);
            this._serverThread.Start();
        }

        public static HttpResponseMessage SendAsync(
            HttpRequestMessage request,
            string method,
            string remote,
            Action<string, string, string, HttpRequestMessage, Exception> requestInfoOnRewritingException = null,
            Action<string, string, HttpRequestMessage, HttpResponseMessage, Exception, string> requestInfoOnRespondingFromRemoteServer = null)
        {
            try
            {
                //todo using task run here now, but it needs to be refactored for performance
                HttpResponseMessage response = Task.Run(() => client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead)).Result;

                requestInfoOnRespondingFromRemoteServer?.Invoke(remote, method, request, response, null, "Request succeeded");
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

                requestInfoOnRespondingFromRemoteServer?.Invoke(remote, method, request, null, e, "Request failed");
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

                requestInfoOnRespondingFromRemoteServer?.Invoke(remote, method, request, null, e, "Sorry, your system does not support the requested feature.");
                return new HttpResponseMessage
                {
                    StatusCode = 0,
                    Content = new StringContent(e.Message)
                };
            }
            catch (TaskCanceledException e)
            {
                requestInfoOnRespondingFromRemoteServer?.Invoke(remote, method, request, null, e, " The request timed out, the endpoint might be unreachable.");

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
                response.Content = new StringContent(message);
                Trace.TraceError("Error:{0}", message);
                requestInfoOnRespondingFromRemoteServer?.Invoke(remote, method, request, response, ex, "Request failed");
                return response;
            }
        }

        static HttpRequestMessage ToHttpRequestMessage(HttpListenerRequest requestInfo, string RewriteToUrl)
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