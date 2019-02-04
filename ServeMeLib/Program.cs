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
        static readonly ConcurrentQueue<KeyValuePair<string, string>> ToExecuteQueue = new ConcurrentQueue<KeyValuePair<string, string>>();
        static readonly ConcurrentDictionary<string, string> PathsWatched = new ConcurrentDictionary<string, string>();
        static bool printResult = true;
        static List<string> urls = new List<string>();
        static ServeMe server;

        static readonly object Padlock = new object();

        static readonly Action helpAction = () =>
        {
            PrintDocTitle("=== ME : PORTS I'M USING ===");
            PrintDoc("to open all the ports im using in default browsers do");
            PrintCode("me");

            PrintDocTitle("=== MAKING HTTP CALLS TO REMOTE SERVER ( WITH DATA ) ===");
            PrintDoc("Enter a request into the system in the format [METHOD] [URI] [(optional)REQUEST_PARAM] [(optional)CONTENT_TYPE]. For example :");
            PrintCode("post http://www.google.com {'name':'cow'} application/json");
            PrintDoc("or simply");
            PrintCode("post http://www.google.com {'name':'cow'}");
            PrintDoc("or in the case og a get request, simply do");
            ConsoleWriteLine("http://www.google.com");
            PrintDoc("Enter 'e' or 'exit' window to exit");
            ConsoleWriteLine("");

            PrintDocTitle("=== EXECUTING CODE INLINE ===");
            PrintDoc("You can also run code (C# Language) inline");
            PrintDoc("For example you can do ");
            PrintCode("code return DateTime.Now;");
            PrintDoc("Or simply");
            PrintCode("code DateTime.Now;");
            ConsoleWriteLine("");

            PrintDocTitle("=== EXECUTING STUFF IN REPITITION ===");
            PrintDoc("You can also run stuff repeatedly by prefixing with 'repeat' ");
            PrintDoc("For example to execute code 10 times pausing for 1000 milliseconds inbetween , do");
            PrintCode("repeat 10 1000 code return System.DateTime.Now;");
            PrintDoc("For example to call get www.google.com 10 times pausing for 1000 milliseconds inbetween , do");
            PrintCode("repeat 10 1000 get http://www.google.com");
            PrintDoc("Or simply");
            PrintCode("repeat 10 1000 http://www.google.com");
            PrintDoc("To run 10 instances of code in parallel with 5 threads");
            PrintCode("repeat 10 parallel 5 code return System.DateTime.Now;");
            ConsoleWriteLine("");

            ConsoleWriteLine("=== RUNNING STUFF IN PARALLEL WITH THREADS ===");
            PrintDoc("To make 10 http get in parallel to google with 5 threads, do ");
            PrintCode("repeat 10 parallel 5 http://www.google.com");
            PrintDoc("That's kind of a load test use case, isn't it?");
            ConsoleWriteLine("");

            PrintDocTitle("=== EXECUTING CODE THAT LIVES IN EXTERNAL PLAIN TEXT FILE ===");
            PrintDoc("You can even execute code that lives externally in a file in plain text");
            PrintDoc("For example, to execute a C# code 50 times in parallel with 49 threads located in a plain text file cs.txt, do ");
            PrintCode("repeat 50 parallel 49 sourcecode cs.txt");
            PrintDoc("Simple but kinda cool eh :) Awesome!");
            ConsoleWriteLine("");

            PrintDocTitle("=== EXECUTING CODE THAT LIVES IN EXTERNAL ASSEMBLY (DLL) FILE ===");
            PrintDoc("You can even execute code that lives externally in an assembly");
            PrintDoc("For example, to execute a C# function called 'DoSomething' with argument 'w' in the class 'ServeMe.Tests.when_serve_me_runs' 50 times in parallel with 49 threads located in an external assembly file  ServeMe.Tests.dll, do ");
            PrintCode("repeat 50 parallel 49 libcode ServeMe.Tests.dll ServeMe.Tests.when_serve_me_runs DoSomething w");
            PrintDoc("If you just want to simply execute a C# function called 'DoSomething' with argument 'w' in the class 'ServeMe.Tests.when_serve_me_runs' located in an external assembly file  ServeMe.Tests.dll, do ");
            PrintCode("libcode ServeMe.Tests.dll ServeMe.Tests.when_serve_me_runs DoSomething w");
            PrintDoc("Now that's dope!");
            ConsoleWriteLine("");

            PrintDocTitle("=== DISABLING VERBOSE MODE ===");
            PrintDoc("To disable inline code result do");
            ConsoleWriteLine("verbose off");
            PrintDoc("You can enable it back by doing");
            ConsoleWriteLine("verbose on");

            PrintDocTitle("=== OPENING DEFAULT BROWSER ===");
            PrintDoc("to open a link in browser do");
            PrintCode("browser http://www.google.com");

            PrintDocTitle("=== ROUTE TO LOCAL HOST ON CURRENT PORT ===");
            PrintDoc("You don't have to enter the host while entering url. Local host will be asumed so if you do 'browser /meandyou' it will open");
            PrintDoc("the default browser to location http://locahost:[PORT]/meandyou");

            PrintDocTitle("=== CURRENT CONFIGURATION / SETUP ===");
            PrintDoc("To see the current routing configuration in use (i.e both contents of server.csv file and those added into memory) do");
            PrintCode("config");
            ConsoleWriteLine("To add config (e.g contains google, http://www.google.com ) in memory , do ");
            ConsoleWriteLine("config contains google, http://www.google.com");

            PrintDocTitle("=== SAVING RESULTS ===");
            PrintDoc("If you want to save the result of a call to an api or of the execution of code , do");
            PrintCode("save index.html http://www.google.com/");
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
go online <--- this exposes your specific folder over the internet
go offline <--- this takes your specific folder offline
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

                    if (server.CanOpenDefaultBrowserOnStart())
                        Process.Start(urls[0]);

                    Console.ForegroundColor = ConsoleColor.White;
                    Console.BackgroundColor = ConsoleColor.Black;
                    server.Log("ServeMe started successfully");
                    ConsoleWriteLine("For help using this small shiny tool , enter 'help' or '?' and enter");

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
                            Console.WriteLine(e.Message + " " + e.InnerException?.Message);
                        }
                    }
                    while (true);
                }
        }

        public static void PrintDocTitle(string title)
        {
            ConsoleWriteLine(
                title,
                () =>
                {
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.ForegroundColor = ConsoleColor.Green;
                });
        }

        public static void PrintDoc(string sample)
        {
            ConsoleWriteLine(
                sample,
                () =>
                {
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                });
        }

        public static void PrintCode(string sample)
        {
            ConsoleWriteLine(
                sample,
                () =>
                {
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.ForegroundColor = ConsoleColor.Green;
                });
        }

        public static void PrintCodeWithDoc(string sample, params string[] dec)
        {
            dec = dec ?? new string[] { };
            foreach (string s in dec)
                ConsoleWriteLine(
                    s,
                    () =>
                    {
                        Console.BackgroundColor = ConsoleColor.Black;
                        Console.ForegroundColor = ConsoleColor.Green;
                    });

            ConsoleWriteLine(
                sample,
                () =>
                {
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.ForegroundColor = ConsoleColor.Green;
                });
        }

        public static void ConsoleWriteLine(string msg, Action setup = null)
        {
            ConsoleColor orF = Console.ForegroundColor;
            ConsoleColor orB = Console.BackgroundColor;
            setup?.Invoke();
            Console.WriteLine(msg);
            Console.ForegroundColor = orF;
            Console.BackgroundColor = orB;
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
                    ConsoleWriteLine($"Save location {saveLocation} not specified or is invalid");
                    return true;
                }
                else
                {
                    if (File.Exists(saveLocation))
                    {
                        ConsoleWriteLine($"File already exist. Are you happy to override file  {saveLocation}?");
                        ConsoleWriteLine("Enter \'y\' for yes and \'n\' for no. If you choose \'y\' the file will be deleted immediately! :");
                        ConsoleWriteLine("Enter \'y\' for yes and \'n\' for no :");
                        string answer = Console.ReadKey().KeyChar.ToString();
                        answer = answer.Trim().ToLower();
                        if (answer == "y")
                        {
                            ConsoleWriteLine($"File {saveLocation} will be overriden");
                            File.Delete(saveLocation);
                        }
                        else if (answer == "n")
                        {
                            ConsoleWriteLine("Good! Nothing will happen");
                            return true;
                        }
                        else
                        {
                            ConsoleWriteLine("I\'ll take that as a \'no\'. Nothing will happen");
                            return true;
                        }
                    }

                    entry = entry.Remove(0, saveLocation.Length).Trim();

                    ConsoleWriteLine($"Result will be saved to {saveLocation}");
                }
            }

            if (entry?.ToLower() == "cheat" || entry?.ToLower() == "?")
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                ConsoleWriteLine("=== CHEAT SHEET ===");
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                ConsoleWriteLine(cheatSheet);

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
                    ConsoleWriteLine("No path supplied");
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
                PathsWatched[pathToWatch] = entry;
                ConsoleWriteLine($"You're all set! When ever anything changes in the path '{pathToWatch}' the command '{entry}' will be executed.");
                return true;
            }

            if (entry.ToLower() == "go online")
            {
                Cleanup();

                //try
                //{
                //    var exitCode =  StartProcess(
                //        $@"cmd.exe",
                //        $@"/K C:\Users\{Environment.UserName}\AppData\Roaming\npm\node_modules\ngrok\bin\ngrok.exe http {server.CurrentPortUsed}",
                //        null,
                //        10000,
                //        Console.Out,
                //        Console.Out).Result;
                //    Console.WriteLine($"Process Exited with Exit Code {exitCode}!");
                //}
                //catch (TaskCanceledException)
                //{
                //    Console.WriteLine("Process Timed Out!");
                //}
                //========================================================================
                //Process greeterProcess = new Process();
                //greeterProcess.StartInfo.FileName = $@"C:\Users\{Environment.UserName}\AppData\Roaming\npm\node_modules\ngrok\bin\ngrok.exe";

                //greeterProcess.StartInfo.RedirectStandardInput = true;

                //greeterProcess.StartInfo.RedirectStandardOutput = true;
                //greeterProcess.StartInfo.UseShellExecute = false;

                //greeterProcess.StartInfo.Arguments = $@"http {server.CurrentPortUsed}";

                //greeterProcess.Start();

                //StreamWriter writer = greeterProcess.StandardInput;

                //StreamReader reader = greeterProcess.StandardOutput;

                //var data = "";
                //do
                //{
                //    data = reader.ReadLine();
                //    ConsoleWriteLine(data);
                //    var goOn = string.IsNullOrWhiteSpace(data);
                //    if (goOn)
                //        break;
                //}
                //while (true);

                //do
                //{
                //    data = reader.ReadLine();
                //    ConsoleWriteLine(data);
                //    var goOn = string.IsNullOrEmpty(data);
                //    if (goOn)
                //        break;
                //}
                //while (true);

                /*
                    using localhost.run free http://localhost.run/

                    ssh -R 80:localhost:31964 ssh.localhost.run
                    The authenticity of host 'ssh.localhost.run (35.13.1.45)' can't be established.
                    RSA key fingerprint is SHA256:FV8IMJ4IYjYUTnd6on7PqbRjaZf4c1EhhEBgeUdE94I.
                    Are you sure you want to continue connecting (yes/no)? y
                    Please type 'yes' or 'no': yes
                    Warning: Permanently added 'ssh.localhost.run,35.13.1.45' (RSA) to the list of known hosts.
                    Connect to http://sa.localhost.run or https://sa.localhost.run
                    Connection to ssh.localhost.run closed.

                    C:\Users\Sa>ssh-keygen -R ssh.localhost.run,35.193.161.204
                    Updating known_hosts is not supported in Windows yet.

                    cd /d "%USERPROFILE%\.ssh

                    */

                /*
                  open status page in browser for ngrok
                  http://localhost:4040/status

                  others to try
                  https://forwardhq.com/pricing
                */
                var psiNpmRunDist = new ProcessStartInfo
                {
                    FileName = "cmd",
                    Arguments = "/c npm list ngrok -g || npm install ngrok -g"
                };
                Process pNpmRunDist = Process.Start(psiNpmRunDist);
                pNpmRunDist.WaitForExit();

                ConsoleWriteLine("Putting your server online now...");
               
                Task.Run(
                    () =>
                    {
                        OnlineProcess = new Process();
                        OnlineProcess.StartInfo.FileName = @"cmd.exe";
                        var ngrokPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"npm\node_modules\ngrok\bin\ngrok.exe");
                        //var p = $@"{Path.GetPathRoot(Environment.SystemDirectory)}Users\{Environment.UserName}\AppData\Roaming\npm\node_modules\ngrok\bin\ngrok.exe";
                        OnlineProcess.StartInfo.Arguments = $@"/c {ngrokPath}  http {server.CurrentPortUsed}";
                        OnlineProcess.Start();
                        ConsoleWriteLine("IMPORTANT: Remember to run 'go offline' to take your server offline!");
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
                //entry = entry.Remove(0, "watchresult".Length).Trim();
                //string result = entry.Split(' ')[0].Trim();

                //if (string.IsNullOrEmpty(result))
                //{
                //    ConsoleWriteLine("No result supplied");
                //    return true;
                //}

                //entry = entry.Remove(0, result.Length).Trim();

                ConsoleWriteLine("Not implemented yet");
                return true;
            }

            if (entry.ToLower() == "config")
            {
                ConsoleWriteLine("Current server configuration is:");
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.DarkGreen;

                string config = server.GetSeUpContent();
                ConsoleWriteLine($"{config}");

                TrySaveResult(saveLocation, config);
                return true;
            }

            if (entry.ToLower().StartsWith("config "))
            {
                entry = entry.Remove(0, "config".Length).Trim();
                if (!string.IsNullOrEmpty(entry))
                {
                    server.AppendToInMemoryConfiguration(entry);
                    ConsoleWriteLine($"The entry '{entry}' has been appended to the configuration");
                }

                return true;
            }

            if (entry?.ToLower() == "me")
            {
                Console.ForegroundColor = ConsoleColor.White;
                ConsoleWriteLine("Server endpoints : ");
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                foreach (string url in urls)
                {
                    ConsoleWriteLine("Opening browser to location " + url);
                    Process.Start(url);
                }

                Console.ForegroundColor = ConsoleColor.White;
                ConsoleWriteLine($"Current server port is {server.GetPortNumberFromSettings()}");
                ConsoleWriteLine("Current server configuration is:");
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                ConsoleWriteLine($"{server.GetSeUpContent()}");

                return true;
            }

            if (entry?.ToLower() == "verbose off")
            {
                printResult = false;
                ConsoleWriteLine("Result of execution will no longer be printed. To reverse this , enter 'verbose on'");
                return true;
            }

            if (entry?.ToLower() == "verbose on")
            {
                printResult = true;
                ConsoleWriteLine("Result of execution will now be printed. To reverse this , enter 'verbose off'");
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
                    ConsoleWriteLine($"Specified repeat counter '{count}' is an invalid number. I was expecting something like this :  repeat 10 1000 code return System.DateTime.Now;");
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
                        ConsoleWriteLine($"Specified max degree of parallelism '{sleepInterval}'ms is invalid. I was expecting something like this :  repeat 10 parallel 5 code return System.DateTime.Now;");
                        return true;
                    }
                }
                else
                {
                    entry = entry.Remove(0, sleepInterval.Length).Trim();

                    if (!int.TryParse(sleepInterval, out sleepInt))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        ConsoleWriteLine($"Specified sleep interval in (ms) '{sleepInterval}'ms is invalid. I was expecting something like this :  repeat 10 1000 code return System.DateTime.Now; ");
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
                                    //  ConsoleWriteLine();
                                    Console.WriteLine(res);
                                }
                            }
                            else if (executionType == "sourcecode")
                            {
                                ConsoleWriteLine($"Loading sourcecode from file and executing it {sourceCodeFilename}...");
                                string source = File.ReadAllText(sourceCodeFilename);

                                object res = SimpleHttpServer.Execute(source);
                                Console.BackgroundColor = ConsoleColor.White;
                                Console.ForegroundColor = ConsoleColor.Black;
                                TrySaveResult(saveLocation, res == null ? "" : new JavaScriptSerializer().Serialize(res));
                                // if (printResult)
                                {
                                    // ConsoleWriteLine();

                                    Console.WriteLine(res);
                                }
                            }
                            else if (executionType == "libcode")
                            {
                                ConsoleWriteLine($"Loading library file and executing it {assemblyFilename}...");

                                //e.g file:///D:/ServeMe.Tests/bin/Debug/ServeMe.Tests.DLL ServeMe.Tests.when_serve_me_runs DoSomething w
                                object res = SimpleHttpServer.InvokeMethod(assemblyFilename, className, methodName, argument);
                                Console.BackgroundColor = ConsoleColor.White;
                                Console.ForegroundColor = ConsoleColor.Black;
                                TrySaveResult(saveLocation, res == null ? "" : new JavaScriptSerializer().Serialize(res));
                                // ConsoleWriteLine();
                                Console.WriteLine(res);
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
                                    ConsoleWriteLine("Opening browser to location " + url);
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

                                    Console.BackgroundColor = ConsoleColor.Black;
                                    Console.ForegroundColor = ConsoleColor.Green;
                                    ConsoleWriteLine($"Sending request with '{method}' to '{url}' as '{mediaType}' with param '{param}' ....");
                                    HttpResponseMessage result = server.MyServer.Send(
                                        request,
                                        server.Log,
                                        e =>
                                        {
                                            Console.ForegroundColor = ConsoleColor.Red;
                                            Console.WriteLine(e.Message + " " + e.InnerException?.Message);
                                        });
                                    Console.BackgroundColor = ConsoleColor.Black;
                                    Console.ForegroundColor = ConsoleColor.Gray;
                                    ConsoleWriteLine($"Obtaining '{method}' response from '{url}' .... ");
                                    Console.BackgroundColor = ConsoleColor.White;
                                    Console.ForegroundColor = ConsoleColor.Black;
                                    TrySaveResult(saveLocation, result.Content.ReadAsStringAsync().Result);

                                    if (printResult)
                                        ConsoleWriteLine(result.Content.ReadAsStringAsync().Result);
                                    else
                                        ConsoleWriteLine($"Completed {webpage}");
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
                                ConsoleWriteLine(
                                    isForever ? $"Will run in next {sleepInt} ms (this will go on forever) ..." : $"Will run in next {sleepInt} ms ({repeatCount} more times to go)...");
                        }

                        Thread.Sleep(sleepInt);
                        return 0;
                    }
                ).ToList();

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.BackgroundColor = ConsoleColor.Black;
            ConsoleWriteLine("OPERATION COMPLETED!");
            return false;
        }

        public static async Task<int> StartProcess(
            string filename,
            string arguments,
            string workingDirectory = null,
            int? timeout = null,
            TextWriter outputTextWriter = null,
            TextWriter errorTextWriter = null)
        {
            using (var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    CreateNoWindow = true,
                    Arguments = arguments,
                    FileName = filename,
                    RedirectStandardOutput = outputTextWriter != null,
                    RedirectStandardError = errorTextWriter != null,
                    UseShellExecute = false,
                    WorkingDirectory = workingDirectory
                }
            })
            {
                process.Start();
                CancellationTokenSource cancellationTokenSource = timeout.HasValue ? new CancellationTokenSource(timeout.Value) : new CancellationTokenSource();

                var tasks = new List<Task>(3) { WaitForExitAsync(process, cancellationTokenSource.Token) };
                if (outputTextWriter != null)
                    tasks.Add(
                        ReadAsync(
                            x =>
                            {
                                process.OutputDataReceived += x;
                                process.BeginOutputReadLine();
                            },
                            x => process.OutputDataReceived -= x,
                            outputTextWriter,
                            cancellationTokenSource.Token));

                if (errorTextWriter != null)
                    tasks.Add(
                        ReadAsync(
                            x =>
                            {
                                process.ErrorDataReceived += x;
                                process.BeginErrorReadLine();
                            },
                            x => process.ErrorDataReceived -= x,
                            errorTextWriter,
                            cancellationTokenSource.Token));

                await Task.WhenAll(tasks);
                return process.ExitCode;
            }
        }

        /// <summary>
        ///     Waits asynchronously for the process to exit.
        /// </summary>
        /// <param name="process">The process to wait for cancellation.</param>
        /// <param name="cancellationToken">
        ///     A cancellation token. If invoked, the task will return
        ///     immediately as cancelled.
        /// </param>
        /// <returns>A Task representing waiting for the process to end.</returns>
        public static Task WaitForExitAsync(
            Process process,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            process.EnableRaisingEvents = true;

            var taskCompletionSource = new TaskCompletionSource<object>();

            EventHandler handler = null;
            handler = (sender, args) =>
            {
                process.Exited -= handler;
                taskCompletionSource.TrySetResult(null);
            };
            process.Exited += handler;

            if (cancellationToken != default(CancellationToken))
                cancellationToken.Register(
                    () =>
                    {
                        process.Exited -= handler;
                        taskCompletionSource.TrySetCanceled();
                    });

            return taskCompletionSource.Task;
        }

        /// <summary>
        ///     Reads the data from the specified data recieved event and writes it to the
        ///     <paramref name="textWriter" />.
        /// </summary>
        /// <param name="addHandler">Adds the event handler.</param>
        /// <param name="removeHandler">Removes the event handler.</param>
        /// <param name="textWriter">The text writer.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static Task ReadAsync(
            Action<DataReceivedEventHandler> addHandler,
            Action<DataReceivedEventHandler> removeHandler,
            TextWriter textWriter,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var taskCompletionSource = new TaskCompletionSource<object>();

            DataReceivedEventHandler handler = null;
            handler = new DataReceivedEventHandler(
                (sender, e) =>
                {
                    if (e.Data == null)
                    {
                        removeHandler(handler);
                        taskCompletionSource.TrySetResult(null);
                    }
                    else
                    {
                        textWriter.WriteLine(e.Data);
                    }
                });

            addHandler(handler);

            if (cancellationToken != default(CancellationToken))
                cancellationToken.Register(
                    () =>
                    {
                        removeHandler(handler);
                        taskCompletionSource.TrySetCanceled();
                    });

            return taskCompletionSource.Task;
        }

        public static string RunCmd(params string[] commands)
        {
            string returnvalue = string.Empty;

            var info = new ProcessStartInfo("cmd");
            info.UseShellExecute = false;
            info.RedirectStandardInput = true;
            info.RedirectStandardOutput = true;
            info.CreateNoWindow = true;

            using (Process process = Process.Start(info))
            {
                StreamWriter sw = process.StandardInput;
                StreamReader sr = process.StandardOutput;

                foreach (string command in commands)
                    sw.WriteLine(command);

                sw.Close();
                returnvalue = sr.ReadToEnd();
            }

            return returnvalue;
        }

        public static int ExecuteCommand(string Command, int Timeout)
        {
            int ExitCode = -1;
            ProcessStartInfo ProcessInfo;
            Process Process;
            try
            {
                ProcessInfo = new ProcessStartInfo("cmd.exe", "/C " + Command);
                ProcessInfo.UseShellExecute = false;
                ProcessInfo.RedirectStandardOutput = true;
                //ProcessInfo.CreateNoWindow = false;
                //ProcessInfo.UseShellExecute = false;
                Process = Process.Start(ProcessInfo);

                Process.BeginOutputReadLine();
                Process.ErrorDataReceived += (sender, eventArgs) => Console.WriteLine(eventArgs.Data);

                Process.OutputDataReceived += new DataReceivedEventHandler(Process_OutputDataReceived);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error Processing ExecuteCommand : " + e.Message);
            }

            return ExitCode;
        }

        static void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine(e.Data);
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
            bool processingStopped = ToExecuteQueue.IsEmpty;

            foreach (KeyValuePair<string, string> keyValuePair in PathsWatched)
            {
                if (!e.FullPath.StartsWith(keyValuePair.Key))
                    continue;
                ConsoleWriteLine($"Detected {e.ChangeType} {e.FullPath}");
                ConsoleWriteLine($"Placing {keyValuePair.Key} into queue to execute '{keyValuePair.Value}' ...");
                ToExecuteQueue.Enqueue(new KeyValuePair<string, string>(keyValuePair.Key, keyValuePair.Value));
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
                        while (!ToExecuteQueue.IsEmpty)
                            if (ToExecuteQueue.TryDequeue(out KeyValuePair<string, string> code))
                            {
                                Console.ForegroundColor = ConsoleColor.Cyan;
                                ConsoleWriteLine($"Because of '{code.Key}'");
                                ConsoleWriteLine($"Execution will begin for instruction '{code.Value}'");
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

            lock (Padlock)
            {
                ConsoleWriteLine($"Saving to {saveLocation}...");
                File.AppendAllText(saveLocation, config + Environment.NewLine);
                ConsoleWriteLine("Saved!");
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

            ConsoleWriteLine($"Running netsh with {parameter} ...");
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
                ConsoleWriteLine("This application needs to run as admin");
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
//    ConsoleWriteLine(e.Data);
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
//        ConsoleWriteLine(e.Data);
//    }
//    catch (Exception exception)
//    {
//        ConsoleWriteLine(exception);
//        //throw;
//    }
//};

// Process.Start($"ngrok  http {server.CurrentPortUsed}");

//OnlineProcess.BeginOutputReadLine();
//OnlineProcess.BeginErrorReadLine();

////string err = OnlineProcess.StandardOutput.ReadToEnd();
////ConsoleWriteLine(err);