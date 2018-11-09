namespace ServeMeLib
{
    using System;
    using System.Diagnostics;
    using System.Reflection;
    using System.Security.Principal;

    class Program
    {
        static void Main(string[] args)
        {
            if (!new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
                TryRunAsAdmin(args);
            else
                using (var server = new ServeMe())
                {
                    server.Start();
                    Console.ForegroundColor = ConsoleColor.White;
                    do
                    {
                        Console.WriteLine("Enter 'e' window to exit");
                    }
                    while (Console.ReadLine()?.Trim().ToLower() != "e");
                }
        }

        public static void TryRunAsAdmin(string[] args)
        {
            var proc = new ProcessStartInfo();
            proc.UseShellExecute = true;
            proc.WorkingDirectory = Environment.CurrentDirectory;
            proc.FileName = Assembly.GetEntryAssembly().CodeBase;

            foreach (string arg in args)
                proc.Arguments += string.Format("\"{0}\" ", arg);

            proc.Verb = "runas";

            try
            {
                Process.Start(proc);
            }
            catch
            {
                Console.WriteLine("This application needs to run as admin");
            }
        }
    }
}