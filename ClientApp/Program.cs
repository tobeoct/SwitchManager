using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using NetworkCommsDotNet;
using NetworkCommsDotNet.Connections;

namespace ClientApp
{
    class Program
    {
        static void Main(string[] args)
        {
            //            //Request server IP and port number
            //            Console.WriteLine("Please enter the server IP and port in the format 192.168.0.1:10000 and press return:");
            //            string serverInfo = Console.ReadLine();
            //
            //            //Parse the necessary information out of the provided string
            //            string serverIP = serverInfo.Split(':').First();
            //            int serverPort = int.Parse(serverInfo.Split(':').Last());
            //
            //            //Keep a loopcounter
            //            int loopCounter = 1;
            //            while (true)
            //            {
            //                //Write some information to the console window
            //                string messageToSend = "This is message #" + loopCounter;
            //                Console.WriteLine("Sending message to server saying '" + messageToSend + "'");
            //
            //                //Send the message in a single line
            //                NetworkComms.SendObject("Message", serverIP, serverPort, messageToSend);
            //
            //                //Check if user wants to go around the loop
            //                Console.WriteLine("\nPress q to quit or any other key to send another message.");
            //                if (Console.ReadKey(true).Key == ConsoleKey.Q) break;
            //                else loopCounter++;
            //            }
            //
            //            //We have used comms so we make sure to call shutdown
            //            NetworkComms.Shutdown();

            //            new System.Net.IPEndPoint(System.Net.IPAddress.Any, 0)

            //Start listening for incoming connections
            //            NetworkComms.AppendGlobalIncomingPacketHandler<string>("Message", PrintIncomingMessage);
            //
            //            CreateEndPoint("server", "127.0.0.1", "85");
            //
            //           
            //
            //            //We have used NetworkComms so we should ensure that we correctly call shutdown
            //            NetworkComms.Shutdown();
//            string serverInfo = "127.0.0.1:90";
//            string serverIp = "localhost";
//            int serverPort = 8080;
//
//
//            //            System.Net.IPAddress remoteIpAddress = System.Net.IPAddress.Parse("0.0.0.0");
//            //            System.Net.IPEndPoint localEndPoint = new System.Net.IPEndPoint(remoteIpAddress,  System.Convert.ToInt16("83", 10));
//            try
//            {
//                TcpClient client = new TcpClient();
//                var serverEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), serverPort);
//                Int64 bytecount = Encoding.ASCII.GetByteCount("Successful");
//                byte[] sendData = new byte[bytecount];
//                sendData = Encoding.ASCII.GetBytes("Successful");
//                Console.WriteLine("Connecting to Server");
//
//                client.Connect(serverEndPoint);
//                NetworkStream stream = client.GetStream();
//                Console.WriteLine("Connected to Server");
//                stream.Write(sendData, 0, sendData.Length);
//                stream.Close();
//                client.Close();
//                Console.ReadKey();
//            }
//            catch (Exception e)
//            {
//                Console.WriteLine(e.ToString());
//                Console.ReadKey();
//            }
           
            
        }
        private static void PrintIncomingMessage(PacketHeader header, Connection connection, string message)
        {
            //            connection.SendObject("MessageReply", new MessageObject(message));
            try
            {
                var serverInfo = connection.ConnectionInfo.LocalEndPoint.ToString();
                string serverIP = serverInfo.Split(':').First();
                int serverPort = int.Parse(serverInfo.Split(':').Last());



                Console.WriteLine("\nA message was received from " + serverInfo + " which said '" + message + "'.");

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

        }
        public static T Deserialize<T>(string toDeserialize, string rootAttr)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(T), new XmlRootAttribute(rootAttr));
            using (StringReader textReader = new StringReader(toDeserialize))
            {
                return (T)xmlSerializer.Deserialize(textReader);
            }
        }
        public static void CreateEndPoint(string type, string IPAddress, string port)
        {
            System.Net.IPEndPoint localEndPoint = null;
            try
            {
                //create a new client socket ...
                Socket m_socWorker = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
                String szIPSelected = IPAddress;
                String szPort = port;
                int alPort = System.Convert.ToInt16(szPort, 10);


                System.Net.IPAddress remoteIpAddress = System.Net.IPAddress.Parse(szIPSelected);
                localEndPoint = new System.Net.IPEndPoint(remoteIpAddress, alPort);
                Console.WriteLine("{0}", localEndPoint);
               
                   Connection.StartListening(ConnectionType.TCP, localEndPoint);
                


                //Print out the IPs and ports we are now listening on
                Console.WriteLine("Server listening for TCP connection on:");



                //Let the user close the server
                Console.WriteLine("\nPress any key to close server.");
                Console.ReadKey(true);

            }
            catch (System.Net.Sockets.SocketException se)
            {
                Console.WriteLine(se.Message);
            }
        }
    }
}
