using System;
using System.Linq;
using System.Net;  
using System.Net.Sockets;  
using System.Text;
using System.Text.RegularExpressions;

public class SocketListener {
  
    private const string PatternPOST = @"([a-z.]+\s)|([0-9.]+\s)";
    private const string PatternGET = @"/resolve\?name=[a-z.0-9]+\&type=(PTR|A)\s+";
    // Incoming data from the client.  
    static byte[] Data;  
    static Socket Listener; 


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
    public static void StartListening(int port) {  
        
        //Create socket for listening
        Listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); 
        try
        {
              
            //Bind socket to port for listening
            Listener.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.1"), port)); 

            //IPHostEntry hostInfo2 = Dns.GetHostEntry("77.75.75.176&"); 
                            
            //Listen for clients, max 5
            Listener.Listen(5); 
            Console.WriteLine("Starting server...");  
            while(true)
            {
                Console.WriteLine("-------------------------");
                Console.WriteLine("Waiting for a connection...");  
                Socket handler = Listener.Accept(); 
                Console.WriteLine("Request received!");  
                
                var data = GetDataFromSocket(ref handler); 
                var method = data.Split(' ').First();
                int ErrorType=0;
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
                            //try parse ip adress from splitted part
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
                    //akakolvek chyba vo formate POSTu -> 400
                    //nenajdeny zaznam -> nevypise sa nic v tom riadku a pokracuje sa
                    case "POST":
                    {
                        Console.WriteLine("Type: POST");
                        var splittedData = data.Split("\r\n\r\n")[1].Split("\n");
                        string adress= "";
                        string type = "";
                        foreach (var item in splittedData)
                        {
                            
                            
                           
                            //check valid pattern
                            var isValidPattern = Regex.IsMatch(item, "[a-z0-9.]+");
                            if(isValidPattern)
                            {
                                var isDelimeter = Regex.IsMatch(item, ":");
                                if(isDelimeter) 
                                {
                                    adress = item.Split(':')[0];
                                    type = item.Split(':')[1];
                                }
                                else ErrorType = 2;
                            } else continue;
                            

                            switch (type)
                            {

                                case "A":
                                {
                                    try
                                    {
                                        IPAddress ipv4Address = GetIPV4Adress(adress);
                                        PostString += adress + ":A=" + ipv4Address.ToString() + "\n";
                                    }
                                    catch 
                                    {
                                        ErrorType = 1;
                                    }
                                   
                                    break;
                                }
                                case "PTR":
                                {
                                    try
                                    {
                                        var hostName = Dns.GetHostEntry(adress).HostName;
                                        PostString += adress + ":PTR=" + hostName + "\n" ;
                                        
                                    }
                                    catch 
                                    {
                                        ErrorType = 1;
                                    }
                                    break;
                                }
                                default:
                                {
                                    ErrorType = 2;
                                    
                                    break;
                                }
                                

                            }
                            
                            
                        }

                        switch (ErrorType)
                        {
                            case 0:
                                SendMsgToClient(ref handler, "HTTP/1.1 200 OK\n\n");
                                SendMsgToClient(ref handler, PostString);
                             break;
                            case 1:
                                SendMsgToClient(ref handler, "HTTP/1.1 404 NotFound\n\n");
                                SendMsgToClient(ref handler, PostString);
                             break;
                            case 2:
                                SendMsgToClient(ref handler, "HTTP/1.1 400 Bad Request\n\n");
                                SendMsgToClient(ref handler, PostString);
                             break;
                            default:
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
            IPAddress ipv4Address = GetIPV4Adress(url);
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

    private static IPAddress GetIPV4Adress(string url)
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