namespace ServeMeLib
{
    using System;

    class Program
    {
        static void Main(string[] args)
        {
            new ServeMe().Start();
            Console.ForegroundColor = ConsoleColor.White;
            do
            {
                Console.WriteLine("Close window to exit");
            }
            while (Console.ReadLine()?.Trim().ToLower() != "e");
        }
    }
}