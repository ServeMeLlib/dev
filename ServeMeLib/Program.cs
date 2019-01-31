namespace ServeMeLib
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Reflection;
    using System.Security.Principal;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web.Script.Serialization;

    class Program
    {
        //http://www.bizcoder.com/these-8-lines-of-code-can-make-debugging-your-asp-net-web-api-a-little-bit-easier
        //todo check autocomplete https://gist.github.com/BKSpurgeon/7f6f28e158032534615773a9a1f73a10
        //netsh http add urlacl url=http://+:62426/ user=SAPC\Sa
        //https://stackoverflow.com/questions/2583347/c-sharp-httplistener-without-using-netsh-to-register-a-uri/2782880#2782880
        static readonly ConcurrentQueue<KeyValuePair<string, string>> toExecuteQueue = new ConcurrentQueue<KeyValuePair<string, string>>();

        static readonly ConcurrentDictionary<string, string> pathsWatched = new ConcurrentDictionary<string, string>();
        static bool printResult = true;
        static List<string> urls = new List<string>();
        static ServeMe server;

        static readonly object padlock = new object();

        static readonly Action helpAction = () =>
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
            Console.WriteLine("repeat 10 1000 code return System.DateTime.Now;");
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
            Console.WriteLine("=== EXECUTING CODE THAT LIVES IN EXTERNAL ASSEMBLY (DLL) FILE ===");

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

        static readonly string cheatSheet =
            @"
=== setup commands ===
cheat <--- display cheat sheet
e <--- exit app
exit <--- exit app
? <---  display cheat sheet
help <--- help
config <--- view current server.csv settings
config [server.csv entry] <--- append to server.csv settings
me <--- open all endpoints in browser
verbose off <--- disable logging certain results
verbose on <--- enable logging certain results

=== inline [command] ===
code [some inline c# code]  <--- execute c# code inline
sourcecode [c# file location] <--- execute c# code location in a file
libcode [.net dll or exe file location] <--- execute a function from a library an executable
browser [url]  <--- open link in default browser
[url]  <--- perform a http get request to url
[get,post,put, etc..] [url] [json arg]   <--- perform http request  to url

=== control commands ===
save [file location] [command]  <--- save result of command execution to file
repeat [count] [interval] [command] <--- repeat command execution one after the other
repeat [count] parallel [no of threads] [command] <--- repeat command execution in parallel
save [file location] repeat [count] [interval] [command] <--- repeat command execution one after the other and appending results to file
save [file location] repeat [count] parallel [no of threads] [command]  <--- repeat command execution in parallel  and appending results to file

=== watches and events ===
watchpath [file or path location] [command] <--- watch directory for changes and execute command when file system changes (create, update, etc) are detected
(NOTE : event may fire multiple times for a single change)
";

        public static Process OnlineProcess { get; set; }

        static void Main(string[] args)
        {
            StartProcessQueue();
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);

            if (!new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
                TryRunAsAdmin(args);
            else
                using (server = new ServeMe())
                {
                    urls = server.Start();

                    string urlToRegister = $"http://*:{server.CurrentPortUsed}/";

                    //registering domain with netsh
                    //ModifyHttpSettings(urlToRegister);

                    if (server.CanOpenDefaultBrowserOnStart())
                        Process.Start(urls[0]);

                    Console.ForegroundColor = ConsoleColor.White;
                    Console.BackgroundColor = ConsoleColor.Black;
                    server.Log("ServeMe started successfully");
                    Console.WriteLine("For help using this small shiny tool , enter 'help' or '?' and enter");

                    do
                    {
                        Console.BackgroundColor = ConsoleColor.Black;
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        try
                        {
                            string entry = Console.ReadLine() ?? "";

                            if (entry?.Trim()?.ToLower() == "e" || entry?.Trim().ToLower() == "exit")
                                break;

                            RunInstruction(entry);
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

        static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            Cleanup();
        }

        static bool RunInstruction(string entry)
        {
            entry = entry.Trim();
            string saveLocation = null;
            if (string.IsNullOrWhiteSpace(entry?.ToLower()))
                return true;

            if (entry.ToLower().StartsWith("save "))
            {
                entry = entry.Remove(0, "save".Length).Trim();
                saveLocation = entry.Split(' ')[0];
                bool isValidFileName = saveLocation != null && string.Concat(saveLocation.Split(Path.GetInvalidFileNameChars())) == saveLocation;
                if (string.IsNullOrEmpty(saveLocation) || !isValidFileName)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Save location {saveLocation} not specified or is invalid");
                    return true;
                }
                else
                {
                    if (File.Exists(saveLocation))
                    {
                        Console.WriteLine($"File already exist. Are you happy to override file  {saveLocation}?");
                        Console.WriteLine("Enter \'y\' for yes and \'n\' for no. If you choose \'y\' the file will be deleted immediately! :");
                        Console.WriteLine("Enter \'y\' for yes and \'n\' for no :");
                        string answer = Console.ReadKey().KeyChar.ToString();
                        answer = answer.Trim().ToLower();
                        if (answer == "y")
                        {
                            Console.WriteLine($"File {saveLocation} will be overriden");
                            File.Delete(saveLocation);
                        }
                        else if (answer == "n")
                        {
                            Console.WriteLine("Good! Nothing will happen");
                            return true;
                        }
                        else
                        {
                            Console.WriteLine("I\'ll take that as a \'no\'. Nothing will happen");
                            return true;
                        }
                    }

                    entry = entry.Remove(0, saveLocation.Length).Trim();

                    Console.WriteLine($"Result will be saved to {saveLocation}");
                }
            }

            if (entry?.ToLower() == "cheat" || entry?.ToLower() == "?")
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("=== CHEAT SHEET ===");
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine(cheatSheet);

                return true;
            }

            if (entry?.ToLower() == "clear" || entry?.ToLower() == "cls")
            {
                Console.Clear();
                return true;
            }

            if (entry?.ToLower() == "help")
            {
                helpAction();
                return true;
            }

            if (entry.ToLower().StartsWith("watchpath "))
            {
                entry = entry.Remove(0, "watchpath".Length).Trim();
                string pathToWatch = entry.Split(' ')[0].Trim();

                if (string.IsNullOrEmpty(pathToWatch))
                {
                    Console.WriteLine("No path supplied");
                    return true;
                }

                pathToWatch = pathToWatch.Replace("//", "\\").Replace("/", "\\").Replace("\\\\", "\\");
                entry = entry.Remove(0, pathToWatch.Length).Trim();

                var _watcher = new FileSystemWatcher
                {
                    Path = pathToWatch,
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.DirectoryName
                };
                _watcher.Created += new FileSystemEventHandler(file_created);
                _watcher.Changed += new FileSystemEventHandler(file_created);
                _watcher.Renamed += new RenamedEventHandler(file_created);
                _watcher.IncludeSubdirectories = true;
                _watcher.EnableRaisingEvents = true;
                pathsWatched[pathToWatch] = entry;
                Console.WriteLine($"You're all set! When ever anything changes in the path '{pathToWatch}' the command '{entry}' will be executed.");
                return true;
            }

            if (entry.ToLower() == "go online")
            {
                Cleanup();

                var psiNpmRunDist = new ProcessStartInfo
                {
                    FileName = "cmd",
                    Arguments = "/c npm install ngrok -g"
                };
                Process pNpmRunDist = Process.Start(psiNpmRunDist);
                pNpmRunDist.WaitForExit();

                Console.WriteLine("Putting your server online now...");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("In the window that will open, look for the url that looks like xxxx.ngrok.io, that will be your public url");
                Console.WriteLine("Enter 'y' to confirm");
                if (Console.ReadKey().KeyChar.ToString() != "y")
                {
                    Console.WriteLine("Going online aborted");
                    return true;
                }

                //https://www.npmjs.com/package/ngrok

                Task.Run(
                    () =>
                    {
                        OnlineProcess = new Process();

                        OnlineProcess.StartInfo.FileName = @"cmd.exe";
                        OnlineProcess.StartInfo.Arguments = $@"/c C:\Users\{Environment.UserName}\AppData\Roaming\npm\node_modules\ngrok\bin\ngrok.exe http {server.CurrentPortUsed}";

                        OnlineProcess.Start();

                        Console.WriteLine("IMPORTANT: Remember to run 'go offline' to take your server offline!");
                        OnlineProcess.WaitForExit();
                    });
                return true;
            }

            if (entry.ToLower() == "go offline")
            {
                try
                {
                    Cleanup();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                return true;
            }

            if (entry.ToLower().StartsWith("watchresult "))
            {
                entry = entry.Remove(0, "watchresult".Length).Trim();
                string result = entry.Split(' ')[0].Trim();

                if (string.IsNullOrEmpty(result))
                {
                    Console.WriteLine("No result supplied");
                    return true;
                }

                entry = entry.Remove(0, result.Length).Trim();

                // file_created(null, new FileSystemEventArgs(WatcherChangeTypes.Changed,));
                // pathsWatched[pathToWatch] = entry;

                Console.WriteLine("Not implemented yet");

                return true;
            }

            if (entry.ToLower() == "config")
            {
                Console.WriteLine("Current server configuration is:");
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.DarkGreen;

                string config = server.GetSeUpContent();
                Console.WriteLine($"{config}");

                TrySaveResult(saveLocation, config);
                return true;
            }

            if (entry.ToLower().StartsWith("config "))
            {
                entry = entry.Remove(0, "config".Length).Trim();
                if (!string.IsNullOrEmpty(entry))
                {
                    server.AppendToInMemoryConfiguration(entry);
                    Console.WriteLine($"The entry '{entry}' has been appended to the configuration");
                }

                return true;
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
                Console.WriteLine("Current server configuration is:");
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine($"{server.GetSeUpContent()}");

                return true;
            }

            if (entry?.ToLower() == "verbose off")
            {
                printResult = false;
                Console.WriteLine("Result of execution will no longer be printed. To reverse this , enter 'verbose on'");
                return true;
            }

            if (entry?.ToLower() == "verbose on")
            {
                printResult = true;
                Console.WriteLine("Result of execution will now be printed. To reverse this , enter 'verbose off'");
                return true;
            }

            Console.ForegroundColor = ConsoleColor.White;
            if (string.IsNullOrEmpty(entry))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                server.Log("Entry cannot be empty");
                return true;
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
                    Console.WriteLine($"Specified repeat counter '{count}' is an invalid number. I was expecting something like this :  repeat 10 1000 code return System.DateTime.Now;");
                    return true;
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
                        return true;
                    }
                }
                else
                {
                    entry = entry.Remove(0, sleepInterval.Length).Trim();

                    if (!int.TryParse(sleepInterval, out sleepInt))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Specified sleep interval in (ms) '{sleepInterval}'ms is invalid. I was expecting something like this :  repeat 10 1000 code return System.DateTime.Now; ");
                        return true;
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
                                // if (printResult)
                                {
                                    //  Console.WriteLine();
                                    Console.WriteLine(res);
                                }
                            }
                            else if (executionType == "sourcecode")
                            {
                                Console.WriteLine($"Loading sourcecode from file and executing it {sourceCodeFilename}...");
                                string source = File.ReadAllText(sourceCodeFilename);

                                object res = SimpleHttpServer.Execute(source);
                                Console.BackgroundColor = ConsoleColor.White;
                                Console.ForegroundColor = ConsoleColor.Black;
                                TrySaveResult(saveLocation, res == null ? "" : new JavaScriptSerializer().Serialize(res));
                                // if (printResult)
                                {
                                    // Console.WriteLine();

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
                                //if (printResult)
                                {
                                    // Console.WriteLine();
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
                                    else
                                        Console.WriteLine($"Completed {webpage}");
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

                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.BackgroundColor = ConsoleColor.Black;

                        {
                            if (printResult)
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
            return false;
        }

        static void Cleanup()
        {
            if (OnlineProcess != null && !OnlineProcess.HasExited)
            {
                OnlineProcess?.Kill();
                OnlineProcess?.WaitForExit();
                OnlineProcess = null;
            }
            else
            {
                OnlineProcess = null;
            }

            foreach (Process process in Process.GetProcessesByName("ngrok"))
            {
                process.Kill();
                process.WaitForExit();
            }
        }

        static void file_created(object sender, FileSystemEventArgs e)
        {
            bool processingStopped = toExecuteQueue.IsEmpty;

            foreach (KeyValuePair<string, string> keyValuePair in pathsWatched)
            {
                if (!e.FullPath.StartsWith(keyValuePair.Key))
                    continue;
                Console.WriteLine($"Detected {e.ChangeType} {e.FullPath}");
                Console.WriteLine($"Placing {keyValuePair.Key} into queue to execute '{keyValuePair.Value}' ...");
                toExecuteQueue.Enqueue(new KeyValuePair<string, string>(keyValuePair.Key, keyValuePair.Value));
            }

            if (processingStopped)
                StartProcessQueue();
        }

        static void StartProcessQueue()
        {
            try
            {
                bool result = Task.Run(
                    () =>
                    {
                        while (!toExecuteQueue.IsEmpty)
                            if (toExecuteQueue.TryDequeue(out KeyValuePair<string, string> code))
                            {
                                Console.ForegroundColor = ConsoleColor.Cyan;
                                Console.WriteLine($"Because of '{code.Key}'");
                                Console.WriteLine($"Execution will begin for instruction '{code.Value}'");
                                RunInstruction(code.Value);
                            }

                        return true;
                    }).Result;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        static void TrySaveResult(string saveLocation, string config)
        {
            if (string.IsNullOrEmpty(saveLocation))
                return;
            if (string.IsNullOrEmpty(config))
                return;

            lock (padlock)
            {
                Console.WriteLine($"Saving to {saveLocation}...");
                File.AppendAllText(saveLocation, config + Environment.NewLine);
                Console.WriteLine("Saved!");
            }
        }

        /// <summary>
        ///     url e.ghttp://+:8888/
        /// </summary>
        /// <param name="url"></param>
        public static void ModifyHttpSettings(string url)
        {
            //https://stackoverflow.com/questions/2521950/wcf-selfhosted-service-installer-class-and-netsh
            string everyone = new SecurityIdentifier(
                "S-1-1-0").Translate(typeof(NTAccount)).ToString();

            //https://www.hanselman.com/blog/WorkingWithSSLAtDevelopmentTimeIsEasierWithIISExpress.aspx
            //https://gilesey.wordpress.com/2013/04/21/allowing-remote-access-to-your-iis-express-service/
            /*
                1. Run a command prompt as administrator, and type in

                netsh http add urlacl url=http://jedi:16253/ user=everyone

                2. Open up the following file in Notepad or Visual Studio

                MyDocuments\IISExpress\config\applicationhost.config

                change the binding from:

                <binding protocol="http" bindingInformation="*:16253:localhost" />

                to:

                <binding protocol="http" bindingInformation="*:16253:jedi" />

                3. Restart the IISExpress service (use the tray icon or task manager, or a command prompt, type issreset).

                4. In Visual Studio, change your Project->Properties->Web settings to launch http://jedi:16253 instead of http://localhost:16253

                5. In a web browser on your development machine type http://jedi:16253/

                6. Assuming all is good – Open up the port on the firewall.

                Goto the Windows Firewall with Advanced Security panel
                Create a new inbound rule
                Click ‘Port’
                Click ‘Next’
                Click TCP
                Enter a specific port 16253
                Click ‘Next’
                Click ‘Allow the connection’
                Click ‘Next’
                Click ‘Next’ (you could untick Public)
                Give it a name “My MVC App” and press Finish.
                7. You should now be able to access the page on the mobile device when connected to the Wi-Fi using http://jedi:16253/
                */
            string parameter = @"http add urlacl url=" + url + @" user=\" + everyone;

            Console.WriteLine($"Running netsh with {parameter} ...");
            var psi = new ProcessStartInfo("netsh", parameter);

            psi.Verb = "runas";
            psi.RedirectStandardOutput = false;
            psi.CreateNoWindow = true;
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            psi.UseShellExecute = false;
            Process.Start(psi);
        }

        static Uri TryReformatUrl(string urlAddressString, List<string> urls)
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

//  OnlineProcess.StartInfo.Verb = "runas";
//OnlineProcess.StartInfo.RedirectStandardError = true;

//OnlineProcess.StartInfo.CreateNoWindow = true;
//OnlineProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
//OnlineProcess.StartInfo.UseShellExecute = false;
// OnlineProcess.StartInfo.RedirectStandardOutput = true;
// OnlineProcess.StartInfo.RedirectStandardInput = true;

//OnlineProcess.StartInfo.CreateNoWindow = true;
//OnlineProcess.StartInfo.RedirectStandardInput = true;
//OnlineProcess.StartInfo.RedirectStandardOutput = true;
//OnlineProcess.StartInfo.UseShellExecute = false;
//OnlineProcess.StartInfo.RedirectStandardError = true;
//OnlineProcess.StartInfo.FileName = "node.exe";
//OnlineProcess.StartInfo.Arguments = "ngrok http {server.CurrentPortUsed}";

//OnlineProcess.ErrorDataReceived += (s, e) =>
//{
//    Console.WriteLine(e.Data);
//};

//OnlineProcess.OutputDataReceived += (s, e) =>
//{
//    try
//    {
//        string rawString = e?.Data?.ToString() ?? "";

//        var urlsOnline = new List<string>();

//        var linkParser = new Regex(@"\b(?:https?://|www\.)\S+\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
//        var matches = linkParser.Matches(rawString);
//        if (matches != null && matches.Count != 0)
//        {
//            foreach (Match m in matches)
//            {
//                if (m.Value.Contains("ngrok"))
//                {
//                    urlsOnline.Add(m.Value);
//                }
//            }
//        }

//        if (urlsOnline.FirstOrDefault() != null)
//        {
//            Process.Start(urlsOnline.First());
//        }
//        Console.WriteLine(e.Data);
//    }
//    catch (Exception exception)
//    {
//        Console.WriteLine(exception);
//        //throw;
//    }
//};

// Process.Start($"ngrok  http {server.CurrentPortUsed}");

//OnlineProcess.BeginOutputReadLine();
//OnlineProcess.BeginErrorReadLine();

////string err = OnlineProcess.StandardOutput.ReadToEnd();
////Console.WriteLine(err);