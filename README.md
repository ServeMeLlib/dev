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

`

----
Picking apart the request
----

Given a request http://www.google.com:456/let/us/go.php?w=tree

/let/us/go.php?w=tree is {{pathandquery}}

/let/us/go.php is {{path}}

www.google.com:456/let/us/go.php?w=tree is {{noscheme}}

https://www.google.com:456/let/us/go.php?w=tree is {{httpsurl}}

http://www.google.com:456/let/us/go.php?w=tree is  {{httpurl}}

http is {{0}} 

www.google.com is {{1}} 

456 is {{2}} 

let is {{3}} 

us is {{4}} 

go.php is {{5}} or {{file}}

w=tree is {{6}} or {{query}}

php is {{extension}}

456 is {{port}}

http is {{scheme}}


http://www.google.com:456 is {{root}}



is {{domain}} www.google.com:456

contains /{{3}}/js/, /{{3}}/js/{{file}}

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
 
Another example, when a POST comes in , turn around and make a GET request to the server 

`EndsWith /google ,  http://www.google.com , POST - GET` 
 
Wrap the result in a jsonp, useful if client makes a jsonp call but server is not setup to return jsonp 

`EndsWith /google ,  http://www.google.com , GETJSONP` 
 
Using filters : when a POST comes in , turn around and make a GET request to the server. When the result comes back, wrap it in jsonp (same effect as above) 

`EndsWith /google ,  http://www.google.com ,  POST - GET | jsonp` 

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

---- To specify https port to use `app sslport,44300` . It will make things easier if you have visual studio installed which will setup ports 44300 to 44399 ports for use with https, otherwise, you have to setup ssl yourself, e.g see https://www.pluralsight.com/blog/software-development/selfcert-create-a-self-signed-certificate-interactively-gui-or-programmatically-in-net  for a convinent self cert tool to use.


---- To open your default browser automatically when you start ServeMe.exe , put this in your server.csv `app openDefaultBrowserOnStartUp`

---- Ok, how about this, imagine you'd like to call a method from an assembly from a dll file. Here is how to do it in server.csv `getSome,assembly file:///D:/ServeMe.Tests/bin/Debug/ServeMe.Tests.DLL ServeMe.Tests.when_serve_me_runs DoSomething w,get` Inside unit tests you can use this helper method to compose the execution instruction `string instruction = ServeMe.GetMethodExecutionInstruction(this.GetType(), methodName, arg);`

---- Ok, finally, how about this, imagine you'd like to execute a c sharp script that lives in a file. Here is how to do it in server.csv `getSome,sourcecode csharp xyz.txt w,get` . This will execute the script when ever a get request is made for /getSome
The script can be something like this
`return DateTime.UtcNow;` inside a file called, say, code.txt
The server entry will be (note that script has access to global variables context and args[] )
`getSome,sourcecode csharp code.cs,get`

---- doing `app classes,helper1.cs,helper2.cs,helper3.cs` will load helper 1 2 and 3  along with any script specified in `sourcecode csharp code.cs` config , and they must be c# classes



---- when executing scripts in `getSome,sourcecode csharp code.cs,get` , you have access to the `_` variable as a global variable which contains helper methods

so a get to /getsome will return the value of  DateTime.UtcNow;



----
This is how a sample server.csv looks like https://github.com/ServeMeLlib/dev/blob/master/server.csv
----
You get the gist :)

From the command line tool , you can enter 'help' or '?' for more information

![image](https://user-images.githubusercontent.com/2102748/52176540-fd398c80-2768-11e9-8dee-5283dea26614.png)
