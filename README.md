# ServeMe
Simple server for testing back-end service


Simply drop it in a folder whose content you want to serve. It's just one exe file .

----

Download the lattest release here https://github.com/ServeMeLlib/dev/releases or get it from nuget 
[![NuGet version](https://badge.fury.io/nu/serveme.svg)](https://badge.fury.io/nu/serveme)


----

Taking a page from the npm package 'serve' , this little app does the same thing :

`
Assuming you would like to serve a static site, single page application or just a static file (no matter if on your device or on the local network), this package is just the right choice for you.

It behaves exactly like static deployments on Now, so it's perfect for developing your static project. Then, when it's time to push it into production, you deploy it.
`

But in addition , to serve json in response to an REST request, create a server.csv file containing url/json mapping of url pathAndQuery mapping to the json filename containing the json that should be served.

----
Ok, so you want to do more, like serve json data like it's coming from a real REST api server ? No worries :) I've got you covered !
----

If you are using server.csv then note that the csv format is :
[ pathAndQuery , some.json , httpMethod  , responseCode ]

For example, to return content or orders.json when GET or POST /GetOrders , then server.csv will contain 
GetOrders , orders.json

Another example, to return content or orders.json when only GET /GetOrders , then server.csv will contain 
GetOrders , orders.json , get

Another example, to return {'orderId':'1001'}  when only POST /UpdateOrder , then server.csv will contain 
UpdateOrder ,  {'orderId':'1001'} , POST

Another example, to return a 404  when only GET /AllData , then server.csv will contain 
UpdateOrder ,  {} , GET , 404

----
This is how a sample server.csv looks like https://github.com/ServeMeLlib/dev/blob/master/server.csv
----
You get the gist :)
