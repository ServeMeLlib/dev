using System.Text.RegularExpressions;

namespace ServeMeLib
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Net.Sockets;
    using System.Reflection;

    public class ServeMe : IDisposable
    {
        public static string Version = "0.37.0";

        internal static Action<Exception, string> _onError = null;
        public static void OnError(Action<Exception, string> handler)
        {
            _onError = handler;
        }

        public string CurrentPath = null;

        public static string TestCurrentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase).Replace("file:/", "").Replace("file:\\", "");
        internal static string CurrentDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location ?? Directory.GetCurrentDirectory());
        internal static string serverFileName = ServeMe.CurrentDirectory + "\\server.csv";
        internal void SetWorkingDirectory(string dir = null)
        {


            if (dir != null)
            {
                CurrentPath = dir;
            }
            else
            {
                CurrentPath = CurrentPath ?? ServeMe.CurrentDirectory;
            }


        }

        private readonly object padlock = new object();
       
        internal Func<string, bool> FileExists = fn => File.Exists(fn);
        internal Func<string, string> ReadAllTextFromFile = fn => File.ReadAllText(fn);
        internal Action<string, string> WriteAllTextToFileIntercept = null;
        internal void WriteAllTextToFile(string fn, string txt)
        {
            if (WriteAllTextToFileIntercept != null)
            {
                WriteAllTextToFileIntercept(fn, txt);
                return;
            }

            if (ServeMe.IsPathRooted(fn))
            {
                File.WriteAllText(fn, txt);
            }
            else
            {
                File.WriteAllText(Path.Combine(CurrentPath, fn), txt);
            }

        }

        internal static bool IsPathRooted(string fn)
        {
            return Path.IsPathRooted(fn)
                   && !Path.GetPathRoot(fn).Equals(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal);
        }
        internal string ServerCsv { set; get; }

        internal SimpleHttpServer MyServer { get; set; }

        public string InMemoryConfigurationAppend { get; private set; }

        public string InMemoryConfigurationPrepend { get; private set; }

        public int CurrentPortUsed { get; set; }

        public void Dispose()
        {
            this.MyServer.Stop();
        }

        public static string GetMethodExecutionInstruction(Type type, string methodName, string arg = "")
        {
            Assembly assembly = type.Assembly;
            string fileName = assembly.CodeBase;
            string className = type.FullName;
            string instruction = $"{fileName} {className} {methodName} {arg}";
            return instruction;
        }

        /// <summary>
        /// Call this method before 'Start'
        /// </summary>
        /// <param name="config"></param>
        public void AppendToInMemoryConfiguration(string config)
        {
            this.InMemoryConfigurationAppend = this.InMemoryConfigurationAppend + "\n" + config;
        }

        public void PrependToInMemoryConfiguration(string config)
        {
            this.InMemoryConfigurationPrepend = config + "\n" + this.InMemoryConfigurationPrepend;
        }

        public string GetSeUpContent()
        {
            this.SetWorkingDirectory();
            this.InMemoryConfigurationPrepend = this.InMemoryConfigurationPrepend ?? "";
            this.InMemoryConfigurationAppend = this.InMemoryConfigurationAppend ?? "";
            if (!string.IsNullOrEmpty(this.ServerCsv) || this.FileExists(ServeMe.serverFileName))
            {
                string content = this.ServerCsv ?? this.ReadAllTextFromFile(ServeMe.serverFileName);
                string[] updateContent;
                if (this.ExtractFromSettings("app LoadSettingsFromFile", content, out updateContent) == 2)
                {
                    string alternatePath = updateContent[1];
                    content = this.ReadAllTextFromFile(alternatePath);
                }

                return this.InMemoryConfigurationPrepend + "\n" + content + "\n" + this.InMemoryConfigurationAppend;
            }

            return this.InMemoryConfigurationPrepend + "\n" + this.InMemoryConfigurationAppend;
        }

        private int ExtractFromSettings(string match, string content, out string[] args)
        {
            match = match.ToLower().Trim();

            if (!string.IsNullOrEmpty(content))
            {
                string[] lns = content.Split('\n');
                foreach (string s in lns)
                {
                    if (string.IsNullOrEmpty(s))
                        continue;
                    string[] lines = s.Split(new[] { "***" }, StringSplitOptions.None)[0].Split(',');
                    if (lines[0].ToLower().Trim() == match)
                    {
                        args = lines;
                        return lines.Length;
                    }
                    else
                    {
                    }
                }
            }

            args = new string[] { };
            return 0;
        }

        private int ExtractFromSettings(string match, out string[] args)
        {
            match = match.ToLower().Trim();
            string content = this.GetSeUpContent().ToLower().Trim();
            if (!string.IsNullOrEmpty(content))
            {
                string[] lns = content.Split('\n');
                foreach (string s in lns)
                {
                    if (string.IsNullOrEmpty(s))
                        continue;
                    string[] lines = s.Split(new[] { "***" }, StringSplitOptions.None)[0].Split(',');
                    if (lines[0].Trim() == match.ToLower())
                    {
                        args = lines;
                        return lines.Length;
                    }
                    else
                    {
                    }
                }
            }

            args = new string[] { };
            return 0;
        }

        public int GetPing(string ip, int timeout)
        {
            int p = -1;
            using (var ping = new Ping())
            {
                PingReply reply = ping.Send(ip, timeout);
                if (reply != null)
                    if (reply.Status == IPStatus.Success)
                        p = Convert.ToInt32(reply.RoundtripTime);
            }

            return p;
        }

        internal string ExecuteTemplate(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }
            string[] data;
            int count = this.ExtractFromSettings("app replacements", out data);
            if (count != 0)
            {
                if (count > 1)

                    try
                    {
                        string fileName = data[1].Trim();

                        if (string.IsNullOrEmpty(fileName))
                        {
                            return input;
                        }

                        string[] templateLines;
                        lock (this.padlock)
                        {
                            templateLines = System.IO.File.ReadAllLines(fileName);
                        }

                        foreach (string templateLine in templateLines)
                        {
                            var parts = templateLine.Split(',');
                            var find = parts[0].Trim();

                            if (parts.Length <= 1)
                                continue;

                            var replace = parts[1].Trim();
                            input = input.Replace(find, replace);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error while trying to perform replacements using " + data[1]);
                        Console.WriteLine(e);
                        ServeMe._onError?.Invoke(e, "Error while trying to perform replacements using " + data[1]);
                    }
            }

            return input;
        }

        internal string[] GetSourceCodeClassNames(params string[] log)
        {
            string[] data;
            int count = this.ExtractFromSettings("app classes", out data);
            if (count != 0)
            {
                if (count > 1)

                {
                    return data.Skip(1).ToArray();
                }


            }

            return new string[] { };
        }
        public  string ToJson(object obj)
        {
            try
            {

                return new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(obj);
            }
            catch (Exception e) { return e.Message; }
        }
        public  object FromJson<T>(string obj)
        {
            try
            {
                return new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<T>(obj);
            }
            catch (Exception e) { return e; }
        }
        public  string Storage(string key, string value = null)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            
            {
                try
                {
                    var db = "ServeMeStore";
                    if (!Directory.Exists(db))
                    {
                        Directory.CreateDirectory(db);
                    }

                    var file = db + "/" + key + ".json";
                    if (!File.Exists(file))
                    {
                        File.WriteAllText(file, "");
                    }

                    if (value != null)
                    {
                        File.WriteAllText(file, value);
                    }
                    return File.ReadAllText(file);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error during db operation " + e.Message);
                    Console.WriteLine(e);
                    ServeMe._onError?.Invoke(e, "Error during db operation" + e.Message);
                }
            }
            return null;
        }
        internal bool Log(params string[] log)
        {
            string[] data;
            int count = this.ExtractFromSettings("app log", out data);
            if (count != 0)
            {
                if (count > 1)
                    lock (this.padlock)
                    {
                        try
                        {
                            string fileName = data[1].Trim();

                            File.AppendAllLines(fileName, log.Select(x => $"[{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)}] {x}"));
                            return true;
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Error while trying to log to " + data[1]);
                            Console.WriteLine(e);
                            ServeMe._onError?.Invoke(e, "Error while trying to log to " + data[1]);
                        }
                    }

                foreach (string s in log)
                    Console.WriteLine($"ServeMe {DateTime.Now} : " + s);
            }
            return true;
        }
        internal static string UrlCombine(string url1, string url2)
        {
            if (url1.Length == 0)
            {
                return url2;
            }

            if (url2.Length == 0)
            {
                return url1;
            }

            url1 = url1.TrimEnd('/', '\\');
            url2 = url2.TrimStart('/', '\\');

            return string.Format("{0}/{1}", url1, url2);
        }
        internal string CanOpenDefaultBrowserOnStart(string root)
        {
            string[] data;
            int count = this.ExtractFromSettings("app openDefaultBrowserOnStartUp", out data);
            if (count != 0)
            {
                if (count > 1)
                {
                    string fileName = data[1].Trim();
                    if (ServeMe.IsPathRooted(fileName))
                    {
                        return fileName;
                    }
                    else
                    {
                        var finalPath = UrlCombine(root, fileName.Replace("\\\\", "/").Replace("\\", "/"));
                        return finalPath;
                    }

                }
                else
                {
                    return root;
                }
            }

            return null;
        }

        //https://stackoverflow.com/questions/570098/in-c-how-to-check-if-a-tcp-port-is-available
        private bool PortInUse(int port)
        {
            bool isAvailable = true;
            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            TcpConnectionInformation[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpConnections();

            foreach (TcpConnectionInformation tcpi in tcpConnInfoArray)
                if (tcpi.LocalEndPoint.Port == port)
                {
                    isAvailable = false;
                    break;
                }

            return isAvailable;
        }

        internal List<List<string>> GetVariablesFromSettings()
        {
            string[] data;
            if (this.ExtractFromSettings("app var", out data) == 2)
            {
                var name = data[1].Split(';').Where(x => !string.IsNullOrEmpty(x)).Select(x => x.Trim().Split('=').Select(y => y.Trim()).ToList()).Where(w => w.Count > 0 && w.Count(string.IsNullOrEmpty) == 0).ToList();
                foreach (var list in name)
                {
                    if (list.Count != 2)
                        throw new Exception($"Variable not well defined  in setting - '{list}' from '{name}'");
                }
                return name;
            }

            return null;
        }

        internal int? GetPortNumberFromSettings()
        {
            string[] data;
            if (this.ExtractFromSettings("app port", out data) == 2)
            {
                int port = int.Parse(data[1]);
                if (this.PortInUse(port))
                    throw new Exception($"Port {port} in setting is in use");
                return port;
            }

            return null;
        }

        internal string GetWorkingPathFromSettings()
        {
            string[] data;
            if (this.ExtractFromSettings("app dir", out data) == 2)
            {
                string dir = data[1].Trim();
                if (!Directory.Exists(dir))
                    throw new Exception($"The directory '{dir}' does not exist");
                return dir;
            }

            return null;
        }

        public List<string> Start(string serverCsv = null, int? port = null, Func<string, bool> fileExists = null, Func<string, string> readAllTextFromFile = null, Action<string, string> writeAllTextToFile = null)
        {
            this.FileExists = fileExists ?? this.FileExists;
            this.ReadAllTextFromFile = readAllTextFromFile ?? this.ReadAllTextFromFile;
            this.WriteAllTextToFileIntercept = writeAllTextToFile;
            this.ServerCsv = serverCsv;
            var endpoints = new List<string>();

            try
            {
                this.SetWorkingDirectory(this.GetWorkingPathFromSettings());
                this.MyServer = new SimpleHttpServer(this, port ?? this.GetPortNumberFromSettings());
                this.CurrentPortUsed = this.MyServer.Port;
                Console.WriteLine("Serving!");
                Console.WriteLine("");
                Console.WriteLine("If you are using server.csv then note that the csv format is :");
                Console.WriteLine("[ pathAndQuery , some.json , httpMethod  , responseCode ]");
                Console.WriteLine("");
                Console.WriteLine("For example, to return content or orders.json when GET or POST /GetOrders do ");
                Console.WriteLine("GetOrders , orders.json");
                Console.WriteLine("");
                Console.WriteLine("Another example, to return content or orders.json when only GET /GetOrders do ");
                Console.WriteLine("GetOrders , orders.json , get ");
                Console.WriteLine("");
                Console.WriteLine("Another example, to return {'orderId':'1001'}  when only POST /UpdateOrder do ");
                Console.WriteLine("UpdateOrder ,  {'orderId':'1001'} , POST");
                Console.WriteLine("");
                Console.WriteLine("Another example, to return a 404  when only GET /AllData do ");
                Console.WriteLine("UpdateOrder ,  {} , GET , 404");
                Console.WriteLine("");

                Console.WriteLine("Another example, to return http://www.google.com content when only GET /google, matching the path and query exactly(not case sensitive) , then server.csv will contain ");
                Console.WriteLine("equalto /google , http://www.google.com , GET");
                Console.WriteLine("");

                Console.WriteLine("Another example, to return http://www.google.com content when only GET and the path and query ends with /google (not case sensitive) , then server.csv will contain");
                Console.WriteLine("EndsWith /google , http://www.google.com , GET");
                Console.WriteLine("");

                Console.WriteLine("To have ServeMe locate your settings file from another location, do ");
                Console.WriteLine("app LoadSettingsFromFile, c:/settings.csv");
                Console.WriteLine("");

                Console.WriteLine("To enable logging do ");
                Console.WriteLine("app log, c:/log.txt");
                Console.WriteLine("");

                Console.WriteLine("This will enable logging to console");
                Console.WriteLine("app log");
                Console.WriteLine("");

                Console.WriteLine("To open default browser when app starts do");
                Console.WriteLine("app openDefaultBrowserOnStartUp");
                Console.WriteLine("");

                Console.WriteLine("For more examples checkout ");
                Console.WriteLine("https://github.com/ServeMeLlib/dev");
                Console.WriteLine("");

                Console.WriteLine("You can access your server through any of the following endpoints :");
                Console.WriteLine("");
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.Green;
                endpoints.Add("http://localhost:" + this.MyServer.Port);
                endpoints.Add("http://127.0.0.1:" + this.MyServer.Port);
                Console.WriteLine("- Local: " + "http://localhost:" + this.MyServer.Port);
                Console.WriteLine("- Local: " + "http://127.0.0.1:" + this.MyServer.Port);
                IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());
                foreach (IPAddress addr in localIPs)
                    if (addr.AddressFamily == AddressFamily.InterNetwork)
                    {
                        endpoints.Add($"http://{addr}:" + this.MyServer.Port);
                        Console.WriteLine($"- On your network: http://{addr}:" + this.MyServer.Port);
                    }

                Console.WriteLine("");
                return endpoints;
            }
            catch (Exception e)
            {
                ServeMe._onError?.Invoke(e, "");
                Console.WriteLine(e);
                return new List<string>();
            }
        }
    }
}