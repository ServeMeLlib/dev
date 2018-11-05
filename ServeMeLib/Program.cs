using System;

namespace ServeMeLib
{
    using System.Diagnostics;

    internal class Program
    {
        private static void Main(string[] args)
        {
            ServeMe.Start();
            do
            {
                Console.WriteLine("Close window to exit");
            }
            while (Console.ReadLine()?.Trim().ToLower() != "e");
        }
    }
}