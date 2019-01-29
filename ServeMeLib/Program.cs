namespace ServeMeLib
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net.Http;
    using System.Reflection;
    using System.Security.Principal;
    using System.Text;
    using System.Threading;

    class Program
    {
        static void Main(string[] args)
        {
            if (!new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
                TryRunAsAdmin(args);
            else
                using (var server = new ServeMe())
                {
                    List<string> urls = server.Start();
                    if (server.CanOpenDefaultBrowserOnStart())
                        Process.Start(urls[0]);
                    string sample = "repeat 10 1000 code return System.DateTime.Now;";

                    Console.ForegroundColor = ConsoleColor.White;
                    Console.BackgroundColor = ConsoleColor.Black;

                    Action helpAction = () =>
                    {

                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.BackgroundColor = ConsoleColor.Black; Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("=== MAKING HTTP CALLS TO REMOTE SERVER ( WITH DATA ) ===");

                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine("Enter a request into the system in the format [METHOD] [URI] [(optional)REQUEST_PARAM] [(optional)CONTENT_TYPE]. For example :");
                        Console.WriteLine("post http://www.google.com {'name':'cow'} application/json");
                        Console.WriteLine("or simply");
                        Console.WriteLine("post http://www.google.com {'name':'cow'}");
                        Console.WriteLine("or in the case og a get request, simply do");
                        Console.WriteLine("http://www.google.com");
                        Console.WriteLine("Enter 'e' or 'exit' window to exit");
                        Console.WriteLine("");
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("=== EXECUTING CODE INLINE ===");

                        Console.ForegroundColor = ConsoleColor.DarkGray;

                        Console.WriteLine("You can also run code (C# Language) inline");
                        Console.WriteLine("For example you can do ");
                        Console.WriteLine("code return DateTime.Now;");
                        Console.WriteLine("Or simply");
                        Console.WriteLine("code DateTime.Now;");
                        Console.WriteLine("");

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("=== EXECUTING STUFF IN REPITITION ===");

                        Console.ForegroundColor = ConsoleColor.DarkGray;

                        Console.WriteLine("You can also run stuff repeatedly by prefixing with 'repeat' ");
                        Console.WriteLine("For example to execute code 10 times pausing for 1000 milliseconds inbetween , do");
                        Console.WriteLine(sample);
                        Console.WriteLine("For example to call get www.google.com 10 times pausing for 1000 milliseconds inbetween , do");
                        Console.WriteLine("repeat 10 1000 get http://www.google.com");
                        Console.WriteLine("Or simply");
                        Console.WriteLine("repeat 10 1000 http://www.google.com");
                        Console.WriteLine("To run 10 instances of code in parallel with 5 threads");
                        Console.WriteLine("repeat 10 parallel 5 code return System.DateTime.Now;");
                        Console.WriteLine(""); Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("=== RUNNING STUFF IN PARALLEL WITH THREADS ===");

                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine("To make 10 http get in parallel to google with 5 threads, do ");
                        Console.WriteLine("repeat 10 parallel 5 http://www.google.com");
                        Console.WriteLine("That's kind of a load test use case, isn't it?");
                        Console.WriteLine("");
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("=== EXECUTING CODE THAT LIVES IN EXTERNAL PLAIN TEXT FILE ===");

                        Console.ForegroundColor = ConsoleColor.DarkGray;

                        Console.WriteLine("You can even execute code that lives externally in a file in plain text");
                        Console.WriteLine("For example, to execute a C# code 50 times in parallel with 49 threads located in a plain text file cs.txt, do ");
                        Console.WriteLine("repeat 50 parallel 49 sourcecode cs.txt");
                        Console.WriteLine("Simple but kinda cool eh :) Awesome!");
                        Console.WriteLine("");
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("=== DISABLING VERBOSE MODE ===");

                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine("To disable inline code result do");
                        Console.WriteLine("verbose off");
                        Console.WriteLine("You can enable it back by doing");
                        Console.WriteLine("verbose on");
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("=== OPENING DEFAULT BROWSER ===");

                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine("to open a link in browser do");
                        Console.WriteLine("browser http://www.google.com");
                    };

                    server.Log("ServeMe started successfully");
                    Console.WriteLine("For help using this small shiny tool , enter 'help' or '?' and enter");


                    string entry = "";
                    bool printResult = true;

                    do
                    {
                        Console.BackgroundColor = ConsoleColor.Black;
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        try
                        {
                            entry = Console.ReadLine() ?? "";
                            entry = entry.Trim();
                            if (entry?.ToLower() == "e" || entry?.Trim().ToLower() == "exit")
                                break;
                            if (entry?.ToLower() == "help" || entry?.ToLower() == "?")
                            {
                                helpAction();
                                continue;
                            }
                            if (entry?.ToLower() == "verbose off")
                            {
                                printResult = false;
                                Console.WriteLine("Result of execution will no longer be printed. To reverse this , enter 'verbose on'");
                                continue;
                            }

                            if (entry?.ToLower() == "verbose on")
                            {
                                printResult = true;
                                Console.WriteLine("Result of execution will now be printed. To reverse this , enter 'verbose off'");
                                continue;
                            }

                            Console.ForegroundColor = ConsoleColor.White;
                            if (string.IsNullOrEmpty(entry))
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                server.Log("Entry cannot be empty");
                                continue;
                            }

                            string executionType = "http";

                            int maxDegreeOfParallelism = 1;
                            int repeatCount = 1;
                            int sleepInt = 0;
                            if (entry.Split(' ')[0].ToLower() == "repeat")
                            {
                                entry = entry.Remove(0, "repeat".Length).Trim();
                                string count = entry.Split(' ')[0].Trim();
                                entry = entry.Remove(0, count.Length).Trim();

                                if (!int.TryParse(count, out repeatCount))
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine($"Specified repeat counter '{count}' is an invalid number. I was expecting something like this :  {sample}");
                                    continue;
                                }

                                string sleepInterval = entry.Split(' ')[0].Trim();

                                if (sleepInterval == "parallel")
                                {
                                    entry = entry.Remove(0, sleepInterval.Length).Trim();

                                    string maxParallel = entry.Split(' ')[0].Trim();
                                    entry = entry.Remove(0, maxParallel.Length).Trim();
                                    if (!int.TryParse(maxParallel, out maxDegreeOfParallelism))
                                    {
                                        Console.ForegroundColor = ConsoleColor.Red;
                                        Console.WriteLine($"Specified max degree of parallelism '{sleepInterval}'ms is invalid. I was expecting something like this :  repeat 10 parallel 5 code return System.DateTime.Now;");
                                        continue;
                                    }
                                }
                                else
                                {
                                    entry = entry.Remove(0, sleepInterval.Length).Trim();

                                    if (!int.TryParse(sleepInterval, out sleepInt))
                                    {
                                        Console.ForegroundColor = ConsoleColor.Red;
                                        Console.WriteLine($"Specified sleep interval in (ms) '{sleepInterval}'ms is invalid. I was expecting something like this :  {sample}");
                                        continue;
                                    }
                                }
                            }

                            string sourceCodeFilename = "";
                            if (entry.Split(' ')[0].ToLower() == "code")
                            {
                                executionType = "code";
                                entry = entry.Remove(0, executionType.Length).Trim();
                            }
                            else if (entry.Split(' ')[0].ToLower() == "sourcecode")
                            {
                                executionType = "sourcecode";
                                entry = entry.Remove(0, executionType.Length).Trim();
                                sourceCodeFilename = entry.Split(' ')[0].Trim();
                                entry = entry.Remove(0, sourceCodeFilename.Length).Trim();
                            }

                            bool isForever = repeatCount == 0;
                            bool willRunAgain = true;

                            Enumerable.Range(0, repeatCount).AsParallel()
                                /*
                                    Parallel works using an under-the-covers concept we refer to as replicating tasks. The concept is that a loop will
                                    start with one task for processing the loop, but if more threads become available to assist in the processing,
                                    additional tasks will be created to run on those threads. This enables minimization of resource consumption.
                                    Given this, it would be inaccurate to state that ParallelOptions enables the specification of a DegreeOfParallelism,
                                    because it’s really a maximum degree: the loop starts with a degree of 1, and may work its way up to any maximum that’s specified as resources become available.

                            PLINQ is different. Some important Standard Query Operators in PLINQ require communication between the threads involved in
                            the processing of the query, including some that rely on a Barrier to enable threads to operate in lock-step. The PLINQ design
                            requires that a specific number of threads be actively involved for the query to make any progress. Thus when you specify a
                            DegreeOfParallelism for PLINQ, you’re specifying the actual number of threads that will be involved, rather than just a maximum.

                                 */
                                .WithDegreeOfParallelism(maxDegreeOfParallelism > repeatCount ? repeatCount : maxDegreeOfParallelism)
                                .Select(
                                    webpage =>
                                    {
                                        try
                                        {
                                            string SpecifiedMethod = "";
                                            if (executionType == "code")
                                            {
                                                object res = SimpleHttpServer.Execute(entry);
                                                Console.BackgroundColor = ConsoleColor.White;
                                                Console.ForegroundColor = ConsoleColor.Black;
                                                if (printResult)
                                                {
                                                    Console.WriteLine();

                                                    Console.WriteLine(res);
                                                }
                                            }
                                            if (executionType == "sourcecode")
                                            {


                                                Console.WriteLine($"Loading sourcecode from file and executing it {sourceCodeFilename}...");
                                                var source = System.IO.File.ReadAllText(sourceCodeFilename);

                                                object res = SimpleHttpServer.Execute(source);
                                                Console.BackgroundColor = ConsoleColor.White;
                                                Console.ForegroundColor = ConsoleColor.Black;
                                                if (printResult)
                                                {
                                                    Console.WriteLine();

                                                    Console.WriteLine(res);
                                                }
                                            }
                                            else
                                            {
                                                string[] entryParts = entry.Split(' ');
                                                HttpMethod method = HttpMethod.Get;
                                                Uri url;
                                                if (entryParts.Length == 1)
                                                {
                                                    url = new Uri(entryParts[0]);
                                                }
                                                else
                                                {
                                                    url = new Uri(entryParts[1]);
                                                    SpecifiedMethod = entryParts[0];

                                                    if (SpecifiedMethod != "browser")
                                                        method = new HttpMethod(SpecifiedMethod);
                                                }

                                                if (SpecifiedMethod == "browser")
                                                {
                                                    Console.WriteLine("Opening browser to location " + url);
                                                    Process.Start(url.ToString());
                                                }
                                                else
                                                {
                                                    var request = new HttpRequestMessage(method, url);

                                                    string param = "";
                                                    if (entryParts.Length > 2)
                                                        param = entryParts[2];

                                                    string mediaType = "application/json";
                                                    if (entryParts.Length > 3)
                                                        mediaType = entryParts[3];
                                                    if (entryParts.Length > 2)
                                                        request.Content = new StringContent(param, Encoding.UTF8, mediaType);
                                                    Console.WriteLine();
                                                    Console.BackgroundColor = ConsoleColor.Black;
                                                    Console.ForegroundColor = ConsoleColor.Green;
                                                    Console.WriteLine($"Sending request with '{method}' to '{url}' as '{mediaType}' with param '{param}' ....");
                                                    HttpResponseMessage result = server.MyServer.Send(
                                                        request,
                                                        server.Log,
                                                        e =>
                                                        {
                                                            Console.ForegroundColor = ConsoleColor.Red;
                                                            Console.WriteLine(e.Message, e.InnerException?.Message);
                                                        });
                                                    Console.BackgroundColor = ConsoleColor.Black;
                                                    Console.ForegroundColor = ConsoleColor.Gray;
                                                    Console.WriteLine($"Obtaining '{method}' response from '{url}' .... ");
                                                    Console.BackgroundColor = ConsoleColor.White;
                                                    Console.ForegroundColor = ConsoleColor.Black;
                                                    Console.WriteLine();
                                                    if (printResult)
                                                        Console.WriteLine(result.Content.ReadAsStringAsync().Result);
                                                }
                                            }
                                        }
                                        catch (Exception e)
                                        {
                                            Console.WriteLine(e);
                                        }

                                        willRunAgain = isForever || --repeatCount > 0;
                                        if (!willRunAgain)
                                            return 0;

                                        Console.WriteLine();
                                        Console.ForegroundColor = ConsoleColor.DarkGray;
                                        Console.BackgroundColor = ConsoleColor.Black;

                                        {
                                            Console.WriteLine(
                                                isForever ? $"Will run in next {sleepInt} ms (this will go on forever) ..." : $"Will run in next {sleepInt} ms ({repeatCount} more times to go)...");
                                        }

                                        Thread.Sleep(sleepInt);
                                        return 0;
                                    }
                                ).ToList();


                            Console.ForegroundColor = ConsoleColor.DarkGreen;
                            Console.BackgroundColor = ConsoleColor.Black;
                            Console.WriteLine("OPERATION COMPLETED!");
                        }
                        catch (Exception e)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine(e.Message, e.InnerException?.Message);
                        }
                    }
                    while (true);
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