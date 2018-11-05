using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServeMeLib
{
    using System.Diagnostics;
    using System.Reflection;

    class Program
    {
        static void Main(string[] args)
        {
            var loc = Assembly.GetEntryAssembly().Location;


            //create server with auto assigned port
            var myServer = new SimpleHttpServer(System.IO.Path.GetDirectoryName(loc));
            Process.Start("http://localhost:" + myServer.Port);
            Console.ReadKey();
        }
    }
}
