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

        StartListening();                         
        return 0;  
    }  
    public static void StartListening() {  
        
        //Create socket for listening
        Listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); 
        try
        {
              
            //Bind socket to port for listening
            Listener.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 6666)); 

            //IPHostEntry hostInfo2 = Dns.GetHostEntry("77.75.75.176&"); 
                            
            //Listen for one client
            Listener.Listen(1); 
            
            while(true)
            {
                Console.WriteLine("Waiting for a connection...");  
                Socket handler = Listener.Accept(); 
                
                var data = GetDataFromSocket(ref handler); 
                //tokezine incoming data into array
                // var tokenizeArray = TokenizeData(ref data,'\n');
                // //find method
                // var method = TokenizeData(ref tokenizeArray[0],' ');
                //get method from string
                var method = data.Split(' ').First();
        
                switch (method)
                {
                    case "GET":
                    {
                        Console.WriteLine("-------------------------\n");
                        Console.WriteLine(data);
                        Console.WriteLine(method);
                        

                        //GET /resolve?name=apple.com&type=A HTTP/1.1
                        //GET /resolve?name="77.75.75.176"type=PTR HTTP/1.1
                       // @"\b/resolve\?name=[a-z.0-9]+\b";
                        Match match = Regex.Match(data, PatternGET);
                        try
                        {
                           if (match != null)
                            {
                                var resolveArg = match.ToString().Split('=')[1];
                                var url = resolveArg.Split('&')[0];
                                //IP
                                if(Char.IsNumber(url[0]))
                                {
                                    bool matchPTR = Regex.IsMatch(match.ToString(), "type=PTR");
                                    if(matchPTR)
                                    {
                                        try
                                        {
                                            var hostName = Dns.GetHostEntry(url).HostName;
                                            SendMsgToClient(ref handler,  "HTTP/1.1 200 OK\r\n\r\n" + url + ":PTR=" + hostName + "\n" );
                                        }
                                        catch (Exception)
                                        {
                                            SendMsgToClient(ref handler, "HTTP/1.1 404 NotFound\r\n\r\n");
                                            
                                        }
                                        
                                    }
                                    else SendMsgToClient(ref handler, "HTTP/1.1 400 Bad Request\r\n\r\n");
                                    //SendMsgToClient(ref handler, "AHHOJ\n");
                                    
                                }
                                //HOST
                                else
                                {
                                    bool matchA = Regex.IsMatch(match.ToString(), "type=A");
                                    if(matchA)
                                    {
                                        try
                                        {
                                            IPAddress ipv4Address = GetIPV4Adress(url);
                                            SendMsgToClient(ref handler, "HTTP/1.1 200 OK\r\n\r\n" + url + ":A=" + ipv4Address.ToString() + "\n");
                                        }
                                        catch (System.Exception)
                                        {
                                            SendMsgToClient(ref handler, "HTTP/1.1 404 NotFound\r\n\r\n");
                                        }
                                        
                                    } else SendMsgToClient(ref handler, "HTTP/1.1 400 Bad Request\r\n\r\n");
                                }
                            }

                            }
                        catch(Exception)
                        {
                            SendMsgToClient(ref handler, "HTTP/1.1 400 Bad Request\r\n\r\n");
                        }
                         
                        Console.WriteLine("-------------------------\n");
                        break;
                    }
                    //akakolvek chyba vo formate POSTu -> 400
                    //nenajdeny zaznam -> nevypise sa nic v tom riadku a pokracuje sa
                    case "POST":
                    {
                        Console.WriteLine(data);
                        Console.WriteLine("-------------------------\n");
                        var splittedData = data.Split("\r\n\r\n")[1].Split("\n");
                        foreach (var item in splittedData)
                        {
                            if(item == "") break;
                            var adress = item.Split(':')[0];
                            var type = item.Split(':')[1];
                            try
                            {
                                switch (type)
                                {

                                    case "A":
                                    {
                                        IPAddress ipv4Address = GetIPV4Adress(adress);
                                        SendMsgToClient(ref handler, adress + ":A=" + ipv4Address.ToString() + "\n");
                                        break;
                                    }
                                    case "PTR":
                                    {
                                        var hostName = Dns.GetHostEntry(adress).HostName;
                                        SendMsgToClient(ref handler,  adress + ":PTR=" + hostName + "\n" );
                                        break;
                                    }
                                    default: 
                                    break;

                                }
                            }
                            catch (System.Exception)
                            {
                                
                                
                            }
                        }
                        SendMsgToClient(ref handler, "AHHOJ\n");
                        Console.WriteLine("-------------------------\n");
                        break;
                    }
                    default:
                    {
                        SendMsgToClient(ref handler, "HTTP/1.1 405 Method Not Allowed\r\n\r\n");
                        break;
                    }
                }
                    handler.Shutdown(SocketShutdown.Both);  
                    handler.Close();  
                 //NETREBA TO VOBEC TOKENIZOVAT, TREBA REGEXY :D 
                
            }
       
        }
        catch (Exception e)
        {
          Console.WriteLine(e.ToString());  
        }
  
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

    public static string TokenizeData(string data, char delimeter) 
    {
        return data.Split(delimeter)[0];
    }


}  