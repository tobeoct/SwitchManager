using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using ClientServerApp.Helper;
using ClientServerApp.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ClientServerApp.PeerConection
{
   public class EndPoints
    {
        private static Socket m_socWorker;
   
        private static DataMessages message;
        private static HelperClassManager helperClassManager;
        public EndPoints()
        {
            m_socWorker = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            helperClassManager= new HelperClassManager();
            message = new DataMessages();
        }


        public bool CreateServer(string address, int port)
        {
            TcpListener server = new TcpListener(IPAddress.Parse(address), port);
            TcpClient client = default(TcpClient);
            try
            {
                server.Start();
                Console.WriteLine($"Server at {address}:{port} Started ...");
                while (true)
                {
                    client = server.AcceptTcpClient();
                    byte[] receivedBuffer = new byte[2000000];
                    NetworkStream stream = client.GetStream();
                    stream.Read(receivedBuffer, 0, receivedBuffer.Length);
                    StringBuilder msg = new StringBuilder();
                    foreach (byte b in receivedBuffer)
                    {
                        if (b.Equals(59))
                        {
                            break;
                        }

                        msg.Append(Convert.ToChar(b).ToString());

                    }

//                    Console.WriteLine("THE MESSAGE \n" + msg.ToString());
//                    Console.WriteLine("THE LENGTH \n" + message);
                    Console.WriteLine("Switch Data Message Recieved");
                    message = helperClassManager.Deserialize<DataMessages>(msg.ToString(), "DataMessages");
                    
                   
//                    message = JsonConvert.DeserializeObject<DataMessages>(msg.ToString());
                    
                    helperClassManager.HandleSwitchConfig(message);
                    Console.Read();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return false;

            }


        }

   
        public static void CreateEndPoint(string IPAddress, string port)
        {
            System.Net.IPEndPoint localEndPoint = null;
            try
            {
                //create a new client socket ...

                String szIPSelected = IPAddress;
                String szPort = port;
                int alPort = System.Convert.ToInt16(szPort, 10);


                System.Net.IPAddress remoteIpAddress = System.Net.IPAddress.Parse(szIPSelected);
                localEndPoint = new System.Net.IPEndPoint(remoteIpAddress, alPort);
                Console.WriteLine("{0}", localEndPoint);
                //                if (type == "server")
                //                {
                //                    Connection.StartListening(ConnectionType.TCP, localEndPoint);
                //                }
                //                

                //Print out the IPs and ports we are now listening on
                Console.WriteLine("Server listening for TCP connection on:");


                //            foreach (System.Net.IPEndPoint localEndPoint in Connection.ExistingLocalListenEndPoints(ConnectionType.TCP))
                //                Console.WriteLine("{0}:{1}", localEndPoint.Address, localEndPoint.Port);

                //Let the user close the server
                Console.WriteLine("\nPress any key to close server.");
                Console.ReadKey(true);

                m_socWorker.Connect(localEndPoint);
            }
            catch (System.Net.Sockets.SocketException se)
            {
                Console.WriteLine(se.Message);
            }
        }
    }
}
