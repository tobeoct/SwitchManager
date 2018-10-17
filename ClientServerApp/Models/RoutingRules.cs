using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using NetworkCommsDotNet;

namespace ClientServerApp.Models
{
    class RoutingRules
    {
        private static string curCombo = "";
        private static SinkNode curSinkNode=null;
        public static bool DetermineFee(int transactionTypeCode, int channelCode, List<TransactionTypeChannelFeeCombo> transactionTypeChannelFeeCombos)
        {
            var partialCombo = transactionTypeCode.ToString().PadLeft(2, '0') + "," + channelCode.ToString();
            foreach (var transactionTypeChannelFeeCombo in transactionTypeChannelFeeCombos)
            {
                string[] words = transactionTypeChannelFeeCombo.Combo.Split(',');
                string trxChannelCombo = words[0] + "," + words[1];
                if (partialCombo.Equals(trxChannelCombo))
                {
                    curCombo = trxChannelCombo+","+words[2];
                    return true;
                }
            }

            return false;


        }
        public static bool DetermineSinkNode(List<TransactionTypeChannelFeeCombo> transactionTypeChannelFeeCombos)
        {
            foreach (var transactionTypeChannelFeeCombo in transactionTypeChannelFeeCombos)
            {
                
                if (curCombo.Equals(transactionTypeChannelFeeCombo.Combo))
                {
                    curSinkNode = transactionTypeChannelFeeCombo.SinkNode;
                    return true;
                }
            }

            return false;
        }
        public static void DetermineRoute(Messages scheme, List<TransactionTypeChannelFeeCombo> transactionTypeChannelFeeCombos)
        {
            if (DetermineFee(scheme.TransactionType, scheme.Channel, transactionTypeChannelFeeCombos))
            {
                if (DetermineSinkNode(transactionTypeChannelFeeCombos))
                {
                    var sinkNodeIP = curSinkNode.IPAddress.ToString();
                    var sinkNodePort = curSinkNode.Port.ToString();
                    var serverInfo = sinkNodeIP + ":" + sinkNodePort;
                    SendMessageToSinkNode(serverInfo);

                }
            }
        }
        public static void ConnectToServer(string serverIp, int serverPort, string message)
        {
            //            var nodes = getSourceNodes();
           

            TcpClient client = new TcpClient();
            var serverEndPoint = new IPEndPoint(IPAddress.Parse(serverIp), serverPort);
            Int64 bytecount = Encoding.ASCII.GetByteCount(message);
            byte[] sendData = new byte[bytecount + 1];
            sendData = Encoding.ASCII.GetBytes(message + ";");
            Console.WriteLine("Connecting to Server");

            client.Connect(serverEndPoint);
            NetworkStream stream = client.GetStream();
            Console.WriteLine("Connected to Server");
            stream.Write(sendData, 0, sendData.Length);
            stream.Close();
            client.Close();
        }
        public static void SendMessageToSinkNode(string serverInfo)
        {
            
            //Request server IP and port number
//            Console.WriteLine("Please enter the server IP and port in the format 192.168.0.1:10000 and press return:");
//           
//            //Parse the necessary information out of the provided string
            string serverIP = serverInfo.Split(':').First();
            int serverPort = int.Parse(serverInfo.Split(':').Last());
            ConnectToServer(serverIP, serverPort, "Sent Successfully");
            Console.WriteLine("Message Sent Successfully to : "+ serverInfo);
//
//
//            //Write some information to the console window
//            NetworkComms.SendObject("MessageReply", serverIP, serverPort, "Sent Successfully");
//
//            Console.WriteLine("Sent the message to : "+serverInfo);
//
//            //We have used comms so we make sure to call shutdown
//            NetworkComms.Shutdown();
        }
    }
}
