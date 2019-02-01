namespace ServeMeLib
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Net.Sockets;
    using System.Reflection;

    public class ServeMe : IDisposable
    {
        public static readonly string CurrentPath = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location ?? Directory.GetCurrentDirectory());

        readonly object padlock = new object();
        readonly string ServerFileName = CurrentPath + "\\server.csv";

        internal Func<string, bool> FileExists = fn => File.Exists(fn);
        internal Func<string, string> ReadAllTextFromFile = fn => File.ReadAllText(fn);

        internal Action<string, string> WriteAllTextToFile = (fn, txt) => File.WriteAllText(fn, txt);

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

        public void AppendToInMemoryConfiguration(string config)
        {
            this.InMemoryConfigurationAppend = this.InMemoryConfigurationAppend + "\n" + config;
        }

        public void PrependToInMemoryConfiguration(string config)
        {
            this.InMemoryConfigurationPrepend = config + "\n" + this.InMemoryConfigurationPrepend;
        }

        internal string GetSeUpContent()
        {
            this.InMemoryConfigurationPrepend = this.InMemoryConfigurationPrepend ?? "";
            this.InMemoryConfigurationAppend = this.InMemoryConfigurationAppend ?? "";
            if (!string.IsNullOrEmpty(this.ServerCsv) || this.FileExists(this.ServerFileName))
            {
                string content = this.ServerCsv ?? this.ReadAllTextFromFile(this.ServerFileName);
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

        int ExtractFromSettings(string match, string content, out string[] args)
        {
            match = match.ToLower().Trim();

            if (!string.IsNullOrEmpty(content))
            {
                string[] lns = content.Split('\n');
                foreach (string s in lns)
                {
                    if (string.IsNullOrEmpty(s))
                        continue;
                    string[] lines = s.Split(',');
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

        int ExtractFromSettings(string match, out string[] args)
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
                    string[] lines = s.Split(',');
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
                            File.AppendAllLines(data[1], log);
                            return true;
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Error while trying to log to " + data[1]);
                            Console.WriteLine(e);
                        }
                    }

                foreach (string s in log)
                    Console.WriteLine($"ServeMe {DateTime.Now} : " + s);
            }

            return true;
        }

        internal bool CanOpenDefaultBrowserOnStart()
        {
            string[] data;
            if (this.ExtractFromSettings("app openDefaultBrowserOnStartUp", out data) != 0)
                return true;

            return false;
        }

        //https://stackoverflow.com/questions/570098/in-c-how-to-check-if-a-tcp-port-is-available
        bool PortInUse(int port)
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

        public List<string> Start(string serverCsv = null, int? port = null, Func<string, bool> fileExists = null, Func<string, string> readAllTextFromFile = null, Action<string, string> writeAllTextToFile = null)
        {
            this.FileExists = fileExists ?? this.FileExists;
            this.ReadAllTextFromFile = readAllTextFromFile ?? this.ReadAllTextFromFile;
            this.WriteAllTextToFile = writeAllTextToFile ?? this.WriteAllTextToFile;
            this.ServerCsv = serverCsv;
            var endpoints = new List<string>();

            try
            {
                this.MyServer = new SimpleHttpServer(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location ?? Directory.GetCurrentDirectory()), this, port ?? this.GetPortNumberFromSettings());
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
                Console.WriteLine(e);
                return new List<string>();
            }
        }
    }
}