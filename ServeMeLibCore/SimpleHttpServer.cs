//namespace ServeMeLibCore
//{
//    // MIT License - Copyright (c) 2016 Can Güney Aksakalli
//    // https://aksakalli.github.io/2014/02/24/simple-http-server-with-csparp.html
//    using System;
//    using System.Collections.Generic;
//    using System.IO;
//    using System.Net;
//    using System.Net.Sockets;
//    using System.Reflection;
//    using System.Text;
//    using System.Text.RegularExpressions;
//    using System.Threading;

//    class SimpleHttpServer
//    {
//        static readonly IDictionary<string, string> _mimeTypeMappings = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
//        {
//            #region extension to MIME type list

//            { ".asf", "video/x-ms-asf" },
//            { ".asx", "video/x-ms-asf" },
//            { ".avi", "video/x-msvideo" },
//            { ".bin", "application/octet-stream" },
//            { ".cco", "application/x-cocoa" },
//            { ".crt", "application/x-x509-ca-cert" },
//            { ".css", "text/css" },
//            { ".deb", "application/octet-stream" },
//            { ".der", "application/x-x509-ca-cert" },
//            { ".dll", "application/octet-stream" },
//            { ".dmg", "application/octet-stream" },
//            { ".ear", "application/java-archive" },
//            { ".eot", "application/octet-stream" },
//            { ".exe", "application/octet-stream" },
//            { ".flv", "video/x-flv" },
//            { ".gif", "image/gif" },
//            { ".hqx", "application/mac-binhex40" },
//            { ".htc", "text/x-component" },
//            { ".htm", "text/html" },
//            { ".html", "text/html" },
//            { ".ico", "image/x-icon" },
//            { ".img", "application/octet-stream" },
//            { ".iso", "application/octet-stream" },
//            { ".jar", "application/java-archive" },
//            { ".jardiff", "application/x-java-archive-diff" },
//            { ".jng", "image/x-jng" },
//            { ".jnlp", "application/x-java-jnlp-file" },
//            { ".jpeg", "image/jpeg" },
//            { ".jpg", "image/jpeg" },
//            { ".js", "application/x-javascript" },
//            { ".mml", "text/mathml" },
//            { ".mng", "video/x-mng" },
//            { ".mov", "video/quicktime" },
//            { ".mp3", "audio/mpeg" },
//            { ".mpeg", "video/mpeg" },
//            { ".mpg", "video/mpeg" },
//            { ".msi", "application/octet-stream" },
//            { ".msm", "application/octet-stream" },
//            { ".msp", "application/octet-stream" },
//            { ".pdb", "application/x-pilot" },
//            { ".pdf", "application/pdf" },
//            { ".pem", "application/x-x509-ca-cert" },
//            { ".pl", "application/x-perl" },
//            { ".pm", "application/x-perl" },
//            { ".png", "image/png" },
//            { ".prc", "application/x-pilot" },
//            { ".ra", "audio/x-realaudio" },
//            { ".rar", "application/x-rar-compressed" },
//            { ".rpm", "application/x-redhat-package-manager" },
//            { ".rss", "text/xml" },
//            { ".run", "application/x-makeself" },
//            { ".sea", "application/x-sea" },
//            { ".shtml", "text/html" },
//            { ".sit", "application/x-stuffit" },
//            { ".swf", "application/x-shockwave-flash" },
//            { ".tcl", "application/x-tcl" },
//            { ".tk", "application/x-tcl" },
//            { ".txt", "text/plain" },
//            { ".war", "application/java-archive" },
//            { ".wbmp", "image/vnd.wap.wbmp" },
//            { ".wmv", "video/x-ms-wmv" },
//            { ".xml", "text/xml" },
//            { ".xpi", "application/x-xpinstall" },
//            { ".zip", "application/zip" },

//            #endregion extension to MIME type list
//        };

//        readonly string[] _indexFiles =
//        {
//            "index.html",
//            "index.htm",
//            "default.html",
//            "default.htm"
//        };

//        HttpListener _listener;
//        int _port;
//        string _rootDirectory;

//        Thread _serverThread;

//        /// <summary>
//        ///     Construct server with given port.
//        /// </summary>
//        /// <param name="path">Directory path to serve.</param>
//        /// <param name="port">Port of the server.</param>
//        public SimpleHttpServer(string path, int port)
//        {
//            this.Initialize(path, port);
//        }

//        /// <summary>
//        ///     Construct server with suitable port.
//        /// </summary>
//        /// <param name="path">Directory path to serve.</param>
//        public SimpleHttpServer(string path, ServeMe serveMe)
//        {
//            this.ServerCsv = serveMe.ServerCsv;
//            //get an empty port
//            var l = new TcpListener(IPAddress.Loopback, 0);
//            l.Start();
//            int port = ((IPEndPoint)l.LocalEndpoint).Port;
//            l.Stop();
//            this.Initialize(path, port);
//        }

//        string ServerCsv { get; }

//        public int Port
//        {
//            get => this._port;
//            private set { }
//        }

//        /// <summary>
//        ///     Stop server and dispose all functions.
//        /// </summary>
//        public void Stop()
//        {
//            this._serverThread.Abort();
//            this._listener.Stop();
//        }

//        void Listen()
//        {
//            this._listener = new HttpListener();
//            this._listener.Prefixes.Add("http://*:" + this._port.ToString() + "/");
//            this._listener.Start();
//            while (true)
//                try
//                {
//                    HttpListenerContext context = this._listener.GetContext();
//                    this.Process(context);
//                }
//                catch (Exception ex)
//                {
//                    Console.WriteLine(ex);
//                }
//        }

//        void Process(HttpListenerContext context)
//        {
//            string filename = context.Request.Url.AbsolutePath;
//            //Console.WriteLine(filename);
//            filename = filename.Substring(1);
//            string currentPath = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location ?? Directory.GetCurrentDirectory());
//            string loc = currentPath + "\\server.csv";

//            if (!string.IsNullOrEmpty(this.ServerCsv) || File.Exists(loc))
//            {
//                string content = this.ServerCsv ?? File.ReadAllText(loc);

//                foreach (string s in content.Split('\n'))
//                {
//                    if (string.IsNullOrEmpty(s))
//                        continue;

//                    string[] parts = s.ToLower().Split(',');
//                    if (parts.Length < 2)
//                        continue;

//                    string from = parts[0].Trim();

//                    if (!new Regex(from).Match(context.Request.Url.PathAndQuery.ToLower().Trim()).Success)
//                        continue;

//                    string to = parts[1].Trim();
//                    filename = to;

//                    if (parts.Length > 2)
//                    {
//                        string expectedMethod = parts[2].Trim();
//                        if (!string.IsNullOrEmpty(expectedMethod))
//                            if (context.Request.HttpMethod.ToLower() != expectedMethod)
//                            {
//                                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
//                                context.Response.OutputStream.Close();
//                                return;
//                            }
//                    }

//                    if (parts.Length > 3 || to.Contains("{"))
//                    {
//                        string responseCode = parts[3].Trim();
//                        if (!string.IsNullOrEmpty(responseCode))
//                        {
//                            int.TryParse(responseCode, out int code);
//                            context.Response.StatusCode = code;
//                        }

//                        if (to.Contains("{"))
//                        {
//                            string responseData = to.Trim();
//                            if (!string.IsNullOrEmpty(responseData))
//                            {
//                                context.Response.ContentType = "application/json";
//                                new MemoryStream(Encoding.Default.GetBytes(responseData)).WriteTo(context.Response.OutputStream);
//                            }
//                        }

//                        context.Response.OutputStream.Close();
//                        return;
//                    }

//                    break;
//                }
//            }

//            if (string.IsNullOrEmpty(filename))
//                foreach (string indexFile in this._indexFiles)
//                    if (File.Exists(Path.Combine(this._rootDirectory, indexFile)))
//                    {
//                        filename = indexFile;
//                        break;
//                    }

//            filename = filename ?? "";
//            if (!filename.Contains(":"))
//                filename = Path.Combine(this._rootDirectory, filename);

//            if (File.Exists(filename))
//                try
//                {
//                    Stream input = new FileStream(filename, FileMode.Open);

//                    //Adding permanent http response headers
//                    string mime;

//                    if (filename.EndsWith(".json"))
//                        context.Response.ContentType = "application/json";
//                    else if (_mimeTypeMappings.TryGetValue(Path.GetExtension(filename), out mime))
//                        context.Response.ContentType = mime;
//                    else
//                        context.Response.ContentType = "application/octet-stream";

//                    context.Response.ContentLength64 = input.Length;
//                    context.Response.AddHeader("Date", DateTime.Now.ToString("r"));
//                    context.Response.AddHeader("Last-Modified", File.GetLastWriteTime(filename).ToString("r"));

//                    var buffer = new byte[1024 * 16];
//                    int nbytes;
//                    while ((nbytes = input.Read(buffer, 0, buffer.Length)) > 0)
//                        context.Response.OutputStream.Write(buffer, 0, nbytes);
//                    input.Close();

//                    context.Response.StatusCode = (int)HttpStatusCode.OK;
//                    context.Response.OutputStream.Flush();
//                }
//                catch (Exception ex)
//                {
//                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
//                }
//            else
//                context.Response.StatusCode = (int)HttpStatusCode.NotFound;

//            context.Response.OutputStream.Close();
//        }

//        void Initialize(string path, int port)
//        {
//            this._rootDirectory = path;
//            this._port = port;
//            this._serverThread = new Thread(this.Listen);
//            this._serverThread.Start();
//        }
//    }
//}