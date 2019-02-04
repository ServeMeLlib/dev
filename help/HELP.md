help
=== ME : PORTS I'M USING ===
to open all the ports im using in default browsers do
`me`
=== MAKING HTTP CALLS TO REMOTE SERVER ( WITH DATA ) ===
Enter a request into the system in the format [METHOD] [URI] [(optional)REQUEST_PARAM] [(optional)CONTENT_TYPE]. For example :
`post http://www.google.com {'name':'cow'} application/json`
or simply
`post http://www.google.com {'name':'cow'}`
or in the case og a get request, simply do
`http://www.google.com`
Enter 'e' or 'exit' window to exit

=== EXECUTING CODE INLINE ===
You can also run code (C# Language) inline
For example you can do
`code return DateTime.Now;`
Or simply
`code DateTime.Now;`

=== EXECUTING STUFF IN REPITITION ===
You can also run stuff repeatedly by prefixing with 'repeat'
For example to execute code 10 times pausing for 1000 milliseconds inbetween , do
`repeat 10 1000 code return System.DateTime.Now;`
For example to call get www.google.com 10 times pausing for 1000 milliseconds inbetween , do
`repeat 10 1000 get http://www.google.com`
Or simply
`repeat 10 1000 http://www.google.com`
To run 10 instances of code in parallel with 5 threads
`repeat 10 parallel 5 code return System.DateTime.Now;`

=== RUNNING STUFF IN PARALLEL WITH THREADS ===
To make 10 http get in parallel to google with 5 threads, do
`repeat 10 parallel 5 http://www.google.com`
That's kind of a load test use case, isn't it?

=== EXECUTING CODE THAT LIVES IN EXTERNAL PLAIN TEXT FILE ===
You can even execute code that lives externally in a file in plain text
For example, to execute a C# code 50 times in parallel with 49 threads located in a plain text file cs.txt, do
`repeat 50 parallel 49 sourcecode cs.txt`
Simple but kinda cool eh :) Awesome!

=== EXECUTING CODE THAT LIVES IN EXTERNAL ASSEMBLY (DLL) FILE ===
You can even execute code that lives externally in an assembly
For example, to execute a C# function called 'DoSomething' with argument 'w' in the class 'ServeMe.Tests.when_serve_me_runs' 50 times in parallel with 49 threads located in an external assembly file  ServeMe.Tests.dll, do
`repeat 50 parallel 49 libcode ServeMe.Tests.dll ServeMe.Tests.when_serve_me_runs DoSomething w`
If you just want to simply execute a C# function called 'DoSomething' with argument 'w' in the class 'ServeMe.Tests.when_serve_me_runs' located in an external assembly file  ServeMe.Tests.dll, do
`libcode ServeMe.Tests.dll ServeMe.Tests.when_serve_me_runs DoSomething w`
Now that's dope!

=== DISABLING VERBOSE MODE ===
To disable inline code result do
`verbose off`
You can enable it back by doing
`verbose on`

=== OPENING DEFAULT BROWSER ===
to open a link in browser do
`browser http://www.google.com`

=== ROUTE TO LOCAL HOST ON CURRENT PORT ===
You don't have to enter the host while entering url. Local host will be asumed so if you do `'browser /meandyou'` it will open
the default browser to location http://locahost:[PORT]/meandyou

=== CURRENT CONFIGURATION / SETUP ===
To see the current routing configuration in use (i.e both contents of server.csv file and those added into memory) do
config
To add config (e.g contains google, http://www.google.com ) in memory , do
`config contains google, http://www.google.com`

=== SAVING RESULTS ===
If you want to save the result of a call to an api or of the execution of code , do
`save index.html http://www.google.com/`