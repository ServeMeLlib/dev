namespace ServeMeLib
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Reflection;
    using System.Security.Principal;
    using System.Text;
    using System.Threading;
    using System.Web.Script.Serialization;

    internal class Program
    {
        //todo check autocomplete https://gist.github.com/BKSpurgeon/7f6f28e158032534615773a9a1f73a10
        //netsh http add urlacl url=http://+:62426/ user=SAPC\Sa
        //https://stackoverflow.com/questions/2583347/c-sharp-httplistener-without-using-netsh-to-register-a-uri/2782880#2782880

        private static void Main(string[] args)
        {
            //ProgramX p = new ProgramX(new string[4] { "Bar", "Barbec", "Barbecue", "Batman" });
            //var rr = p.RunProgram();
            //p.ma

            if (!new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
                TryRunAsAdmin(args);
            else
                using (var server = new ServeMe())
                {
                    List<string> urls = server.Start();

                    var urlToRegister = $"http://*:{server.CurrentPortUsed}/";

                    //registering domain with netsh
                    ModifyHttpSettings(urlToRegister);

                    if (server.CanOpenDefaultBrowserOnStart())
                        Process.Start(urls[0]);
                    string sample = "repeat 10 1000 code return System.DateTime.Now;";

                    Console.ForegroundColor = ConsoleColor.White;
                    Console.BackgroundColor = ConsoleColor.Black;

                    Action helpAction = () =>
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGray;

                        Console.BackgroundColor = ConsoleColor.Black;
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("=== ME : PORTS I'M USING ===");
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine("to open all the ports im using in default browsers do");
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.WriteLine("me");
                        Console.ForegroundColor = ConsoleColor.DarkGray;

                        Console.BackgroundColor = ConsoleColor.Black;
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("=== MAKING HTTP CALLS TO REMOTE SERVER ( WITH DATA ) ===");

                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine("Enter a request into the system in the format [METHOD] [URI] [(optional)REQUEST_PARAM] [(optional)CONTENT_TYPE]. For example :");
                        Console.ForegroundColor = ConsoleColor.Blue;

                        Console.WriteLine("post http://www.google.com {'name':'cow'} application/json");
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine("or simply");
                        Console.ForegroundColor = ConsoleColor.Blue;

                        Console.WriteLine("post http://www.google.com {'name':'cow'}");
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine("or in the case og a get request, simply do");
                        Console.ForegroundColor = ConsoleColor.Blue;

                        Console.WriteLine("http://www.google.com");
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine("Enter 'e' or 'exit' window to exit");
                        Console.WriteLine("");
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("=== EXECUTING CODE INLINE ===");

                        Console.ForegroundColor = ConsoleColor.DarkGray;

                        Console.WriteLine("You can also run code (C# Language) inline");
                        Console.WriteLine("For example you can do ");
                        Console.ForegroundColor = ConsoleColor.Blue;

                        Console.WriteLine("code return DateTime.Now;");
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine("Or simply");
                        Console.ForegroundColor = ConsoleColor.Blue;

                        Console.WriteLine("code DateTime.Now;");
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine("");

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("=== EXECUTING STUFF IN REPITITION ===");

                        Console.ForegroundColor = ConsoleColor.DarkGray;

                        Console.WriteLine("You can also run stuff repeatedly by prefixing with 'repeat' ");
                        Console.WriteLine("For example to execute code 10 times pausing for 1000 milliseconds inbetween , do");
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.WriteLine(sample);
                        Console.ForegroundColor = ConsoleColor.DarkGray;

                        Console.WriteLine("For example to call get www.google.com 10 times pausing for 1000 milliseconds inbetween , do");
                        Console.ForegroundColor = ConsoleColor.Blue;

                        Console.WriteLine("repeat 10 1000 get http://www.google.com");
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine("Or simply");
                        Console.ForegroundColor = ConsoleColor.Blue;

                        Console.WriteLine("repeat 10 1000 http://www.google.com");
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine("To run 10 instances of code in parallel with 5 threads");
                        Console.ForegroundColor = ConsoleColor.Blue;

                        Console.WriteLine("repeat 10 parallel 5 code return System.DateTime.Now;");
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine("");
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("=== RUNNING STUFF IN PARALLEL WITH THREADS ===");

                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine("To make 10 http get in parallel to google with 5 threads, do ");
                        Console.ForegroundColor = ConsoleColor.Blue;

                        Console.WriteLine("repeat 10 parallel 5 http://www.google.com");
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine("That's kind of a load test use case, isn't it?");
                        Console.WriteLine("");
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("=== EXECUTING CODE THAT LIVES IN EXTERNAL PLAIN TEXT FILE ===");

                        Console.ForegroundColor = ConsoleColor.DarkGray;

                        Console.WriteLine("You can even execute code that lives externally in a file in plain text");
                        Console.WriteLine("For example, to execute a C# code 50 times in parallel with 49 threads located in a plain text file cs.txt, do ");
                        Console.ForegroundColor = ConsoleColor.Blue;

                        Console.WriteLine("repeat 50 parallel 49 sourcecode cs.txt");
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine("Simple but kinda cool eh :) Awesome!");
                        Console.WriteLine("");

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("=== EXECUTING CODE THAT LIVES IN EXTERNAL ASSEBLY (DLL) FILE ===");

                        Console.ForegroundColor = ConsoleColor.DarkGray;

                        Console.WriteLine("You can even execute code that lives externally in an assembly");
                        Console.WriteLine("For example, to execute a C# function called 'DoSomething' with argument 'w' in the class 'ServeMe.Tests.when_serve_me_runs' 50 times in parallel with 49 threads located in an external assembly file  ServeMe.Tests.dll, do ");

                        Console.ForegroundColor = ConsoleColor.Blue;

                        Console.WriteLine("repeat 50 parallel 49 libcode ServeMe.Tests.dll ServeMe.Tests.when_serve_me_runs DoSomething w");
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine("If you just want to simply execute a C# function called 'DoSomething' with argument 'w' in the class 'ServeMe.Tests.when_serve_me_runs' located in an external assembly file  ServeMe.Tests.dll, do ");
                        Console.ForegroundColor = ConsoleColor.Blue;

                        Console.WriteLine("libcode ServeMe.Tests.dll ServeMe.Tests.when_serve_me_runs DoSomething w");
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine("Now that's dope!");
                        Console.WriteLine("");

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("=== DISABLING VERBOSE MODE ===");

                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine("To disable inline code result do");
                        Console.ForegroundColor = ConsoleColor.Blue;

                        Console.WriteLine("verbose off");
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine("You can enable it back by doing");
                        Console.ForegroundColor = ConsoleColor.Blue;

                        Console.WriteLine("verbose on");
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("=== OPENING DEFAULT BROWSER ===");

                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine("to open a link in browser do");
                        Console.ForegroundColor = ConsoleColor.Blue;

                        Console.WriteLine("browser http://www.google.com");
                        Console.ForegroundColor = ConsoleColor.DarkGray;

                        Console.WriteLine("=== ROUTE TO LOCAL HOST ON CURRENT PORT ===");

                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine("You don't have to enter the host while entering url. Local host will be asumed so if you do 'browser /meandyou' it will open");
                        Console.WriteLine("the default browser to location http://locahost:[PORT]/meandyou");

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("=== CURRENT CONFIGURATION / SETUP ===");

                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine("To see the current routing configuration in use (i.e both contents of server.csv file and those added into memory) do");
                        Console.ForegroundColor = ConsoleColor.Blue;

                        Console.WriteLine("config");
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine("To add config (e.g contains google, http://www.google.com ) in memory , do ");
                        Console.ForegroundColor = ConsoleColor.Blue;

                        Console.WriteLine("config contains google, http://www.google.com");
                        Console.ForegroundColor = ConsoleColor.DarkGray;

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("=== SAVING RESULTS ===");

                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine("If you want to save the result of a call to an api or of the execution of code , do");
                        Console.ForegroundColor = ConsoleColor.Blue;

                        Console.WriteLine("save index.html http://www.google.com/");
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                    };

                    server.Log("ServeMe started successfully");
                    Console.WriteLine("For help using this small shiny tool , enter 'help' or '?' and enter");

                    string entry = "";
                    bool printResult = true;
                    string saveLocation = null;
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

                            if (entry.ToLower().StartsWith("save "))
                            {
                                entry = entry.Remove(0, "save".Length).Trim();
                                saveLocation = entry.Split(' ')[0];
                                var isValidFileName = saveLocation != null && String.Concat(saveLocation.Split(Path.GetInvalidFileNameChars())) == saveLocation;
                                if (string.IsNullOrEmpty(saveLocation) || !isValidFileName)
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine($"Save location {saveLocation} not specified or is invalid");
                                    continue;
                                }
                                else
                                {
                                    if (System.IO.File.Exists(saveLocation))
                                    {
                                        Console.WriteLine($"File already exist. Are you happy to override file  {saveLocation}?");
                                        Console.WriteLine($"Enter 'y' for yes and 'n' for no. If you choose 'y' the file will be deleted immediately! :");
                                        Console.WriteLine($"Enter 'y' for yes and 'n' for no :");
                                        var answer = Console.ReadKey().KeyChar.ToString();
                                        answer = answer.Trim().ToLower();
                                        if (answer == "y")
                                        {
                                            Console.WriteLine($"File {saveLocation} will be overriden");
                                            System.IO.File.Delete(saveLocation);
                                        }
                                        else if (answer == "n")
                                        {
                                            Console.WriteLine($"Good! Nothing will happen");
                                            continue;
                                        }
                                        else
                                        {
                                            Console.WriteLine($"I'll take that as a 'no'. Nothing will happen");
                                            continue;
                                        }
                                    }
                                    entry = entry.Remove(0, saveLocation.Length).Trim();

                                    Console.WriteLine($"Result will be saved to {saveLocation}");
                                }
                            }

                            if (entry?.ToLower() == "help" || entry?.ToLower() == "?")
                            {
                                helpAction();
                                continue;
                            }

                            if (entry.ToLower() == "config")
                            {
                                Console.WriteLine($"Current server configuration is:");
                                Console.BackgroundColor = ConsoleColor.Black;
                                Console.ForegroundColor = ConsoleColor.DarkGreen;

                                var config = server.GetSeUpContent();
                                Console.WriteLine($"{config}");

                                TrySaveResult(saveLocation, config);
                                continue;
                            }
                            if (entry.ToLower().StartsWith("config "))
                            {
                                entry = entry.Remove(0, "config".Length).Trim();
                                if (!string.IsNullOrEmpty(entry))
                                {
                                    server.AppendToInMemoryConfiguration(entry);
                                    Console.WriteLine($"The entry '{entry}' has been appended to the configuration");
                                }
                                continue;
                            }

                            if (entry?.ToLower() == "me")
                            {
                                Console.ForegroundColor = ConsoleColor.White;
                                Console.WriteLine("Server endpoints : ");
                                Console.ForegroundColor = ConsoleColor.DarkGreen;
                                foreach (string url in urls)
                                {
                                    Console.WriteLine("Opening browser to location " + url);
                                    Process.Start(url);
                                }
                                Console.ForegroundColor = ConsoleColor.White;
                                Console.WriteLine($"Current server port is {server.GetPortNumberFromSettings()}");
                                Console.WriteLine($"Current server configuration is:");
                                Console.BackgroundColor = ConsoleColor.Black;
                                Console.ForegroundColor = ConsoleColor.DarkGreen;
                                Console.WriteLine($"{server.GetSeUpContent()}");

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
                            string assemblyFilename = "";
                            string className = "";
                            string methodName = "";
                            string argument = null;
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
                            else if (entry.Split(' ')[0].ToLower() == "libcode")
                            {
                                executionType = "libcode";
                                entry = entry.Remove(0, executionType.Length).Trim();
                                assemblyFilename = entry.Split(' ')[0].Trim();
                                entry = entry.Remove(0, assemblyFilename.Length).Trim();
                                className = entry.Split(' ')[0].Trim();
                                entry = entry.Remove(0, className.Length).Trim();
                                methodName = entry.Split(' ')[0].Trim();
                                entry = entry.Remove(0, methodName.Length).Trim();
                                argument = entry.Split(' ')[0].Trim();
                                entry = entry.Remove(0, argument.Length).Trim();
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
                                            string specifiedMethod = "";
                                            if (executionType == "code")
                                            {
                                                object res = SimpleHttpServer.Execute(entry);
                                                Console.BackgroundColor = ConsoleColor.White;
                                                Console.ForegroundColor = ConsoleColor.Black;

                                                TrySaveResult(saveLocation, res == null ? "" : new JavaScriptSerializer().Serialize(res));
                                                if (printResult)
                                                {
                                                    Console.WriteLine();
                                                    Console.WriteLine(res);
                                                }
                                            }

                                            if (executionType == "sourcecode")
                                            {
                                                Console.WriteLine($"Loading sourcecode from file and executing it {sourceCodeFilename}...");
                                                string source = File.ReadAllText(sourceCodeFilename);

                                                object res = SimpleHttpServer.Execute(source);
                                                Console.BackgroundColor = ConsoleColor.White;
                                                Console.ForegroundColor = ConsoleColor.Black;
                                                TrySaveResult(saveLocation, res == null ? "" : new JavaScriptSerializer().Serialize(res));
                                                if (printResult)
                                                {
                                                    Console.WriteLine();

                                                    Console.WriteLine(res);
                                                }
                                            }
                                            else if (executionType == "libcode")
                                            {
                                                Console.WriteLine($"Loading library file and executing it {assemblyFilename}...");

                                                //e.g file:///D:/ServeMe.Tests/bin/Debug/ServeMe.Tests.DLL ServeMe.Tests.when_serve_me_runs DoSomething w
                                                object res = SimpleHttpServer.InvokeMethod(assemblyFilename, className, methodName, argument);
                                                Console.BackgroundColor = ConsoleColor.White;
                                                Console.ForegroundColor = ConsoleColor.Black;
                                                TrySaveResult(saveLocation, res == null ? "" : new JavaScriptSerializer().Serialize(res));
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
                                                    string urlAddressString = entryParts[0].Trim();
                                                    url = TryReformatUrl(urlAddressString, urls);
                                                }
                                                else
                                                {
                                                    url = TryReformatUrl(entryParts[1], urls);
                                                    specifiedMethod = entryParts[0];
                                                    if (specifiedMethod != "browser")
                                                        method = new HttpMethod(specifiedMethod);
                                                }

                                                if (specifiedMethod == "browser")
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
                                                    TrySaveResult(saveLocation, result.Content.ReadAsStringAsync().Result);
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
        static object padlock = new object();
        private static void TrySaveResult(string saveLocation, string config)
        {
            if (string.IsNullOrEmpty(saveLocation))
                return;
            if (string.IsNullOrEmpty(config))
            {
                return;
            }

            lock (padlock)
            {
                Console.WriteLine($"Saving to {saveLocation}...");
                System.IO.File.AppendAllText(saveLocation, config + Environment.NewLine);
                Console.WriteLine($"Saved!");
            }

        }

        /// <summary>
        /// url e.ghttp://+:8888/
        /// </summary>
        /// <param name="url"></param>
        public static void ModifyHttpSettings(string url)
        {
            //https://stackoverflow.com/questions/2521950/wcf-selfhosted-service-installer-class-and-netsh
            string everyone = new System.Security.Principal.SecurityIdentifier(
                "S-1-1-0").Translate(typeof(System.Security.Principal.NTAccount)).ToString();

            string parameter = @"http add urlacl url=" + url + @" user=\" + everyone;

            Console.WriteLine($"Running netsh with {parameter} ...");
            ProcessStartInfo psi = new ProcessStartInfo("netsh", parameter);

            psi.Verb = "runas";
            psi.RedirectStandardOutput = false;
            psi.CreateNoWindow = true;
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            psi.UseShellExecute = false;
            Process.Start(psi);
        }

        private static Uri TryReformatUrl(string urlAddressString, List<string> urls)
        {
            Uri url;
            if (urlAddressString.StartsWith("www."))
                urlAddressString = "http://" + urlAddressString;

            if (urlAddressString.StartsWith("/"))
                urlAddressString = urls[0] + urlAddressString;

            url = new Uri(urlAddressString);
            return url;
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