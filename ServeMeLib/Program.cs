namespace ServeMeLib
{
    using System;
    using System.Diagnostics;
    using System.Linq;

    class Program
    {
        static void Main(string[] args)
        {
            using (var server = new ServeMe())
            {
                server.Start();
                //Process.Start(server.Start().First() + "/GetOrders2");
                Console.ForegroundColor = ConsoleColor.White;
                do
                {
                    Console.WriteLine("Enter 'e' window to exit");
                }
                while (Console.ReadLine()?.Trim().ToLower() != "e");
            }
        }
    }
}