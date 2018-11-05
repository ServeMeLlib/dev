namespace ServeMeLib
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Sockets;
    using System.Reflection;

    public class ServeMe
    {
        public static List<string> Start(string directory=null)
        {
            var endpoints=new List<string>();
            var myServer = new SimpleHttpServer(directory??System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));
            Console.WriteLine($"Serving!");
            Console.WriteLine($"");
            endpoints.Add("http://localhost:" + myServer.Port);
            endpoints.Add("http://127.0.0.1:" + myServer.Port);
            Console.WriteLine($"- Local: " + "http://localhost:" + myServer.Port);
            Console.WriteLine($"- Local: " + "http://127.0.0.1:" + myServer.Port);
            IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());
            foreach (IPAddress addr in localIPs)
            {
                if (addr.AddressFamily == AddressFamily.InterNetwork)
                {
                    endpoints.Add("http://{addr}:" + myServer.Port);
                    Console.WriteLine($"- On your network: http://{addr}:" + myServer.Port);
                }
            }
            Console.WriteLine($"");
            return endpoints;
        }
    }
}