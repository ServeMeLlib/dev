=== CHEAT SHEET ===


=== setup commands ===

`cheat` <--- display cheat sheet

`e` <--- exit app

`exit` <--- exit app

`?` <---  display cheat sheet

`help` <--- help

`config` <--- view current server.csv settings

`config` [server.csv entry] <--- append to server.csv settings

`me` <--- open all endpoints in browser

`verbose off` <--- disable logging certain results

`verbose on` <--- enable logging certain results


=== inline [command] ===

`code [some inline c# code]`  <--- execute c# code inline

`sourcecode [c# file location]` <--- execute c# code location in a file

`libcode [.net dll or exe file location]` <--- execute a function from a library an executable

`browser [url]`  <--- open link in default browser

`[url]`  <--- perform a http get request to url

`[get,post,put, etc..] [url] [json arg]`   <--- perform http request  to url


=== control commands ===

`go online` <--- this exposes your specific folder over the internet

`go offline` <--- this takes your specific folder offline

`save [file location] [command]`  <--- save result of command execution to file

`repeat [count] [interval] [command]` <--- repeat command execution one after the other

`repeat [count] parallel [no of threads] [command]` <--- repeat command execution in parallel

`save [file location] repeat [count] [interval] [command]` <--- repeat command execution one after the other and appending results to file

`save [file location] repeat [count] parallel [no of threads] [command]`  <--- repeat command execution in parallel  and appending results to file


=== watches and events ===

`watchpath [file or path location] [command]` <--- watch directory for changes and execute command when file system changes (create, update, etc) are detected

(NOTE : event may fire multiple times for a single change)
