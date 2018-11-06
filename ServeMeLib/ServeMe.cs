﻿namespace ServeMeLib
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Reflection;

    public class ServeMe
    {
        internal string ServerCsv { set; get; }

        public List<string> Start(string directory = null, string serverCsv = null)
        {
            this.ServerCsv = serverCsv;
            var endpoints = new List<string>();
            var myServer = new SimpleHttpServer(directory ?? Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location ?? Directory.GetCurrentDirectory()), this);
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
            Console.WriteLine("You can access your server through any of the following endpoints :");
            Console.WriteLine("");
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Green;
            endpoints.Add("http://localhost:" + myServer.Port);
            endpoints.Add("http://127.0.0.1:" + myServer.Port);
            Console.WriteLine("- Local: " + "http://localhost:" + myServer.Port);
            Console.WriteLine("- Local: " + "http://127.0.0.1:" + myServer.Port);
            IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());
            foreach (IPAddress addr in localIPs)
                if (addr.AddressFamily == AddressFamily.InterNetwork)
                {
                    endpoints.Add($"http://{addr}:" + myServer.Port);
                    Console.WriteLine($"- On your network: http://{addr}:" + myServer.Port);
                }

            Console.WriteLine("");
            return endpoints;
        }
    }
}