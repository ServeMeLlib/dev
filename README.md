# ServeMe
Simple server for testing ( and mocking) back-end service


Simply drop it in a folder whose content you want to serve. It's just one exe file 

----

Download the lattest release here https://github.com/ServeMeLlib/dev/releases or get it from nuget 

[![NuGet version](https://badge.fury.io/nu/serveme.svg)](https://badge.fury.io/nu/serveme)


----

Taking a page from the npm package 'serve' , this little app does the same thing :

`
Assuming you would like to serve a static site, single page application or just a static file (no matter if on your device or on the local network), this package is just the right choice for you.

It behaves exactly like static deployments on Now, so it's perfect for developing your static project. Then, when it's time to push it into production, you deploy it.
`


----
Ok, so you want to do more, like serve json data like it's coming from a real REST api server ? No worries :) I've got you covered !
----

To serve json in response to an REST request, create a server.csv file containing url/json mapping of url pathAndQuery mapping to the json filename containing the json that should be served.

If you are using server.csv then note that the csv format is :
[ pathAndQuery , some.json , httpMethod  , responseCode ]

Note : that you can use regular expressions to pattern match the PathAndQuery !

For example, to return content or orders.json when GET or POST /GetOrders , then server.csv will contain 

`GetOrders , orders.json`

Another example, to return content or orders.json when only GET /GetOrders , then server.csv will contain 

`GetOrders , orders.json , get`

Another example, to return {'orderId':'1001'}  when only POST /UpdateOrder , then server.csv will contain 

`UpdateOrder , json {'orderId':'1001'} , POST`

Another example, to return a 404  when only GET /AllData , then server.csv will contain 

`AllData , json  {} , GET , 404`

Another example, to return {'orderId':'1001'}  when only POST /UpdateOrder, matching the path and query exactly , then server.csv will contain 

`equalto /UpdateOrder ,  json {'orderId':'1001'} , POST`

Another example, to return http://www.google.com content  when only GET /google, matching the path and query exactly(not case sensitive) , then server.csv will contain

`equalto /google ,  http://www.google.com , GET`
 
Another example, to return http://www.google.com content  when only GET and the path and query ends with /google (not case sensitive) , then server.csv will contain 

`EndsWith /google ,  http://www.google.com , GET`     

Another example, to return http://www.google.com content  when only GET and the path and query starts with /google (not case sensitive) , then server.csv will contain 

`StartsWith /google ,  http://www.google.com , GET` 
 
Another example, to return http://www.google.com content  when only GET and the path and query contains /google (not case sensitive) , then server.csv will contain 

`Contains /google ,  http://www.google.com , GET` 

Another example, to return http://www.google.com content  when only GET and the path and query matches using regular expression with /go(.*) (not case sensitive) , then server.csv will contain 

`Regex /go(.*)  ,  http://www.google.com , GET` 

In order to negate any of the above, simply add ! prefix. For example,
to return http://www.google.com content  when only GET and the path and query does NOT start with /google (not case sensitive) , then server.csv will contain 

`!StartsWith /google ,  http://www.google.com , GET` 

 Notice the '!' prefix

---- Please note that the default is `regex` when ever nothing is specified

---- If you want to relocate your server settings to another folder , put this in your server.csv `app LoadSettingsFromFile,path/to/file/nameOfFile.cxv`

---- To enable logging to file put this in your server.csv `app log, log.txt` and to log only to console , so `app log` exclude the file name.

---- To specify a port number to use, put this in your server.csv `app port,8080` . The port must not be in use.

---- To open your default browser automatically when you start ServeMe.exe , put this in your server.csv `app openDefaultBrowserOnStartUp`

---- Ok, how about this, imagine you'd like to call a method from an assembly from a dll file. Here is how to do it in server.csv `getSome,assembly file:///D:/ServeMe.Tests/bin/Debug/ServeMe.Tests.DLL ServeMe.Tests.when_serve_me_runs DoSomething w,get` Inside unit tests you can use this helper method to compose the execution instruction `string instruction = ServeMe.GetMethodExecutionInstruction(this.GetType(), methodName, arg);`

---- Ok, finally, how about this, imagine you'd like to execute a c sharp script that lives in a file. Here is how to do it in server.csv `getSome,sourcecode csharp xyz.txt w,get` . This will execute the script when ever a get request is made for /getSome
The script can be something like this
`return DateTime.UtcNow;` inside a file called, say, code.txt
The server entry will be 
`getSome,sourcecode csharp code.txt,get`

so a get to /getsome will return the value of  DateTime.UtcNow;



----
This is how a sample server.csv looks like https://github.com/ServeMeLlib/dev/blob/master/server.csv
----
You get the gist :)

From the command line tool , you can enter 'help' or '?' for more information

![image](https://user-images.githubusercontent.com/2102748/52176540-fd398c80-2768-11e9-8dee-5283dea26614.png)
