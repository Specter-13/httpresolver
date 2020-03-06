using System;
using System.Linq;
using System.Net;  
using System.Net.Sockets;  
using System.Text;
using System.Text.RegularExpressions;

public class Resolver {
  
  
    private const string PatternPOST = @"POST /dns-query HTTP/1.1\s*$";
    private const string PatternGET = @"/resolve\?name=[a-z.0-9]+\&type=(PTR|A)\s+";
    // Incoming data from the client.  
    static byte[] Data;  
    static Socket Listener; 
    enum ErrorType
    {
        BadRequest,
        NotFound,
        ItIsOkay
    }
    public static int Main(String[] args) 
    {  
        Console.WriteLine("Welcome to xspavo00 server!");
        if(args.Length == 0)
        {
            Console.WriteLine("ERROR! No port specified!");  
        }
        else
        {
            try
            {
            int port = Int32.Parse(args[0]);
            if(port >= 0 && port <= 65535 )
            { 
                Console.WriteLine("Listening port: " + port);  
                StartListening(port);    
            }  
            else Console.WriteLine($"ERROR! Port out of range (0-65535).");

            }
            catch
            {
                Console.WriteLine($"ERROR! Unable to parse '{args[0]}'.");
            }
        }
                           
        return 0;  
    }  
//---------------------------------------------------------------------    
 
    public static void StartListening(int port) {  
        
        //Create socket for listening
        Listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); 
        try
        {
              
            //Bind socket to port for listening
            Listener.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.1"), port)); 
                            
            //Listen for clients, max 10
            Listener.Listen(10); 
            Console.WriteLine("Starting server...");  
            var errorType = ErrorType.BadRequest;
            while(true)
            {
                Console.WriteLine("-------------------------");
                Console.WriteLine("Waiting for a connection...");  
                Socket handler = Listener.Accept(); 
                Console.WriteLine("Request received!");  
                
                var data = GetDataFromSocket(ref handler); 
                var method = data.Split(' ').First();
                string PostString = "";
        
                switch (method)
                {
                    case "GET":
                    {
                       Console.WriteLine("Type: GET");
                       
  
                        Match match = Regex.Match(data, PatternGET);
                        //check for correct pattern of request
                        if (match.Success)
                        {
                            //split arguments by delimeters
                            var resolveArg = match.ToString().Split('=')[1];
                            var url = resolveArg.Split('&')[0];
                            //try parse ip address from splitted part
                            bool isIP = IPAddress.TryParse ( url, out System.Net.IPAddress address);
                            //if is IP. check for type and resolve
                            if(isIP)
                            {
                                handler = ResolveIP(handler, match, url);
                            }
                            //else is HOSTNAME
                            else
                            {
                                //check for type
                                bool matchA = Regex.IsMatch(match.ToString(), "type=A");
                                if(matchA)
                                {
                                    //resolve
                                    handler = ResolveHostName(handler, url);

                                }
                                else SendMsgToClient(ref handler, "HTTP/1.1 400 Bad Request\n\n");
                            }
                        }
                        else
                        {
                            SendMsgToClient(ref handler, "HTTP/1.1 400 Bad Request\n\n");
                        }

                        
                        
                      
                        break;
                    }
                   
                    case "POST":
                    {
                        Console.WriteLine("Type: POST");
                        var firstLine = data.Split("\n")[0];
                        var splittedData = data.Split("\r\n\r\n")[1].Split("\n");
                        bool IsValidPost = Regex.IsMatch(firstLine, PatternPOST);
                        if(IsValidPost)
                        {
                            foreach (var item in splittedData)
                            {
                                string address = "";
                                string type = "";

                                //check valid pattern
                                var isValidPattern = Regex.IsMatch(item, "[a-z0-9.]+");
                                if(isValidPattern)
                                {
                                    var isDelimeter = Regex.IsMatch(item, ":");
                                    if(isDelimeter) 
                                    {
                                        address = item.Split(':')[0];
                                        type = item.Split(':')[1];
                                    }
                                    else 
                                    {
                                        if(errorType == ErrorType.ItIsOkay) continue;
                                        else errorType = ErrorType.BadRequest;
                                    }
                                } else continue;
                                

                                switch (type)
                                {

                                    case "A":
                                    {
                                        try
                                        {
                                            IPAddress ipv4Address = GetIPV4Address(address);
                                            if(ipv4Address.ToString() == address) continue;
                                            PostString += address + ":A=" + ipv4Address.ToString() + "\n";
                                            errorType = ErrorType.ItIsOkay;
                                        }
                                        catch 
                                        {
                                            if(errorType == ErrorType.ItIsOkay) continue;
                                            else errorType = ErrorType.NotFound;
                                        }
                                    
                                        break;
                                    }
                                    case "PTR":
                                    {
                                        try
                                        {
                                            var hostName = Dns.GetHostEntry(address).HostName;
                                            if(hostName == address) continue;
                                            PostString += address + ":PTR=" + hostName + "\n" ;
                                            errorType = ErrorType.ItIsOkay;
                                            
                                        }
                                        catch 
                                        {
                                            if(errorType == ErrorType.ItIsOkay) continue;
                                            else errorType = ErrorType.NotFound;
                                        }
                                        break;
                                    }
                                    default:
                                    {
                                        if(errorType == ErrorType.ItIsOkay) continue;
                                        else errorType = ErrorType.BadRequest;
                                        
                                        break;
                                    }
                                    

                                }
                                
                                
                            }
                            
                        }
                        else errorType = ErrorType.BadRequest;

                        switch (errorType)
                        {
                            case ErrorType.ItIsOkay:
                                SendMsgToClient(ref handler, "HTTP/1.1 200 OK\n\n");
                                SendMsgToClient(ref handler, PostString);
                             break;
                            case ErrorType.NotFound:
                                SendMsgToClient(ref handler, "HTTP/1.1 404 NotFound\n\n");
                                SendMsgToClient(ref handler, PostString);
                             break;
                            case ErrorType.BadRequest:
                                SendMsgToClient(ref handler, "HTTP/1.1 400 Bad Request\n\n");
                                SendMsgToClient(ref handler, PostString);
                             break;
                            
                        }
                        break;
                    }
                    default:
                    {
                        SendMsgToClient(ref handler, "HTTP/1.1 405 Method Not Allowed\n\n");
                        break;
                    }
                }
            handler.Shutdown(SocketShutdown.Both);  
            handler.Close(); 

            }
        }
        catch (Exception e)
        {
          Console.WriteLine(e.ToString());  
        }
  
    }
    //                                                                                                          
    private static Socket ResolveHostName(Socket handler, string url)
    {
        try
        {
            IPAddress ipv4Address = GetIPV4Address(url);
            SendMsgToClient(ref handler, "HTTP/1.1 200 OK\n\n");
            SendMsgToClient(ref handler, url + ":A=" + ipv4Address.ToString() + "\n");
        }
        catch (System.Exception)
        {
            SendMsgToClient(ref handler, "HTTP/1.1 404 NotFound\n\n");
        }

        return handler;
    }

    private static Socket ResolveIP(Socket handler, Match match, string url)
    {
        bool matchPTR = Regex.IsMatch(match.ToString(), "type=PTR");
        if (matchPTR)
        {
            try
            {
                var hostName = Dns.GetHostEntry(url).HostName;
                SendMsgToClient(ref handler, "HTTP/1.1 200 OK\n\n");
                SendMsgToClient(ref handler, url + ":PTR=" + hostName + "\n");

            }
            catch (Exception)
            {
                SendMsgToClient(ref handler, "HTTP/1.1 404 NotFound\n\n");

            }

        }
        else SendMsgToClient(ref handler, "HTTP/1.1 400 Bad Request\n\n");
        return handler;
    }

    private static IPAddress GetIPV4Address(string url)
    {
        return Array.Find(
        Dns.GetHostEntry(url).AddressList,
        a => a.AddressFamily == AddressFamily.InterNetwork);
        
    }

    public static string GetDataFromSocket(ref Socket handler) 
    {  
        int i = 0;
        Data = new byte[handler.SendBufferSize]; 
                
        int incomingBufferSize = handler.Receive(Data); 
        var buffer = new byte[incomingBufferSize];     

        while(i < incomingBufferSize)      
        { 
            buffer[i] = Data[i];
            i++;
        }          
        var data = Encoding.Default.GetString(buffer); 
        return data;
    }
    public static void SendMsgToClient(ref Socket handler, string msg) 
    {  
       byte[] msgString = Encoding.Default.GetBytes(msg); 
        handler.Send(msgString);  
        
    }


}  