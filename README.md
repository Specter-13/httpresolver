![logo](logo.png "logo")
# **IPK project 1 - HTTP domain names resolver**  
**Dávid Špavor**  
**xspavo00**  

Simple server programmed in .NET core which supports GET and POST requests.
Server is running on localhost. Make sure **internet connection** is available which is needed for translation of IP or host name, otherwise application won't work as expected! Server can listen to maximum of 10 clients.

# **Usage:**

- make build

- make run PORT=1234

PORT number is between 0 - 65535.

- CTRL+C for terminating server



# **Implementation:**

Project is implemented in one file *Resolver.cs*, which contain various methods:

- `public static int Main(String[] args)`  
    - Main method.
    - Arguments checking.
- `public static void StartListening(int port)`  
    - Contains socket handling and while(true) statement, where server is running.
    - Argument `port` specifies port, where server is listening.
    - On connection received, received data are evaluated and parsed. Based on evaluation requested method is handled.
    - Incoming data are evaluated by Regular Expressions and switch statements.
    - On success, result data with HTTP header are sent back to client and connection will be closed.
    - On failure, correct error HTTP header is returned to client.
- `private static Socket ResolveHostName(Socket handler, string url)` 
    - Tries to resolve incoming host name to ip address.
    - On failure, 404 Not Found header is returned to client.
- `private static Socket ResolveIP(Socket handler, Match match, string url)`
    - Tries to resolve incoming ip address to host name.
    - On failure, 404 Not Found header is returned to client.
- `private static IPAddress GetIPV4Address(string url)`
    - Return resolved IPv4 address, because IPAddress type contains array of IPv4 or IPv6 addresses .
- `public static string GetDataFromSocket(ref Socket handler)` 
    - Decode received data encoded in Bytes to string and return them in correct format, which is needed for further parsing.
- `public static void SendMsgToClient(ref Socket handler, string msg)`
    - Method for sending messages to client.    

# **Format of requests and Testing**
## Format of requests
`GET /resolve?name=apple.com&type=A HTTP/1.1`  
`POST /dns-query HTTP/1.1`
## Testing
Application was tested by utility curl.  
Example:

`curl localhost:5353/resolve?name=www.fit.vutbr.cz\&type=A`  

Response:  

    www.fit.vutbr.cz:A=147.229.9.23  

POST:  

`curl --data-binary @queries.txt -X POST http://localhost:5353/dns-query`  

Where queries.txt contains:  

    www.fit.vutbr.cz:A  
    www.google.com:A  
    www.seznam.cz:A  
    147.229.14.131:PTR  
    ihned.cz:A  

Response:  

    www.fit.vutbr.cz:A=147.229.9.23
    www.google.com:A=216.58.201.68
    www.seznam.cz:A=77.75.74.176
    147.229.14.131:PTR=dhcpz131.fit.vutbr.cz
    ihned.cz:A=46.255.231.42



# **Important notes:**
## Post method
**Please wait for `"Waiting for connection.."` statement on server console. Only when this statement is showed, requests can be sent!**  
  
File, which is required for sending data with POST method, is handled as follows:  
- Empty lines are ignored.
- If at least one translation of query is successful, 200 OK is always returned despite others errors in file, so wrong lines are skipped.
- If empty file is received, 400 Bad Request is returned.
- If all queries are wrong, correct error header is returned.


# **Reference:**
[Socket programming Microsoft Documentation](https://docs.microsoft.com/en-us/dotnet/framework/network-programming/sockets)

# **Contacts:**
<xspavo00@stud.fit.vutbr.cz>