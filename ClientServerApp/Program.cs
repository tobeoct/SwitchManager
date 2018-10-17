using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using ClientServerApp.Models;
using NetworkCommsDotNet;
using NetworkCommsDotNet.Connections;
using Switch.WebAPI.Models;
using Trx.Messaging;
using Trx.Messaging.Channels;
using Trx.Messaging.Iso8583;
using Trx.Messaging.FlowControl;
using log4net.Plugin;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SwitchConsole.Models;

//using TcpListener = System.Net.Sockets.TcpListener;

namespace ClientServerApp
{
    class Program
    {
        private static Socket m_socWorker;
        private static List<SourceNode> sourceNodes;
        private static List<TransactionTypeChannelFeeCombo> combos;
        private static List<Fee> fees;
        private static Messages message;

        static void Main(string[] args)
        {
         
            CreateServer("nodes", "127.0.0.1", 8080);

        }
        public static bool CreateServer(string type, string address, int port)
        {
            System.Net.Sockets.TcpListener server = new System.Net.Sockets.TcpListener(IPAddress.Parse(address), port);
            TcpClient client = default(TcpClient);
            try
            {
                server.Start();
                Console.WriteLine("Server Started ...");
                while (true)
                {
                    client = server.AcceptTcpClient();
                    byte[] receivedBuffer = new byte[2000000];
                    NetworkStream stream = client.GetStream();
                    stream.Read(receivedBuffer, 0, receivedBuffer.Length);
                    StringBuilder msg = new StringBuilder();
                    //                    string msg = Encoding.ASCII.GetString(receivedBuffer, 0, receivedBuffer.Length);
                    foreach (byte b in receivedBuffer)
                    {
                        if (b.Equals(59))
                        {
                            break;
                        }
                        else
                        {
                            msg.Append(Convert.ToChar(b).ToString());
                        }
                    }
                    Console.WriteLine("THE MESSAGE \n" + msg.ToString());
                    Console.WriteLine("THE LENGTH \n" + msg.Length);
                    if (type == "nodes")
                    {
                        sourceNodes = Deserialize<List<SourceNode>>(msg.ToString(), "ArrayOfSourceNode"
                        );

                        HandleNodes(sourceNodes);
                    }
                    else
                    {
                        message = Deserialize<Messages>(msg.ToString(), "Message");
                        RoutingRules.DetermineRoute(message, combos);

                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return false;
                
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

        public static void HandleNodes(List<SourceNode> sourceNodes)

        {
            combos = new List<TransactionTypeChannelFeeCombo>();
            foreach (var node in sourceNodes)
            {
                var trxCombo = new TransactionTypeChannelFeeCombo();
                trxCombo.Combo = CreateTrxTypeChannelFeeCombo(node.Scheme);
                trxCombo.SinkNode = node.Scheme.Route.SinkNode;
                combos.Add(trxCombo);
                //                if (node.Status == "Active")
                //                {
                //                    CreateServer("other", node.IPAddress, Int32.Parse(node.Port));
                //                }
                if (node.Status == "Active")
                {

                    TRXPeerSetup(node);
                   
                }

            }
        }

        public static void TRXPeerSetup(SourceNode node)
        {
            Trx.Messaging.FlowControl.TcpListener tcpListener = new Trx.Messaging.FlowControl.TcpListener(node.Port);
            tcpListener.LocalInterface = node.IPAddress;
            ListenerPeer listener = new ListenerPeer(node.Id,
                new TwoBytesNboHeaderChannel(new Iso8583Ascii1987BinaryBitmapMessageFormatter()),
                new BasicMessagesIdentifier(11, 41),
                tcpListener);
            ListenerPeer
            listener.Error += (sender, e) => ErrorListener(sender, e);
            listener.Receive += (sender, e) => ReceiveListener(sender, e, node);
            listener.Disconnected += (sender, e) => DisconnectListener(sender, node);
            listener.Connect();
            Console.WriteLine($"Now listening to {node.Name} on IP {node.IPAddress} port {node.Port}");
        }

        private static void ErrorListener(object sender, Trx.Utilities.ErrorEventArgs e)
        {
            var i = 0;
        }
     

//        private static void ReceiveListener(object sender, ReceiveEventArgs e, SourceNode node)
//        {
//            var msg = e.Message;
//            Console.WriteLine(e.Message);
//            message = Deserialize<Messages>(msg.ToString(), "Message");
//            RoutingRules.DetermineRoute(message, combos);
//            //            throw new NotImplementedException();
//        }
        private static void ReceiveListener(object sender, ReceiveEventArgs e, SourceNode node)
        {
            Console.WriteLine($"Received message from {node.Name} node");
            ProcessTransaction(e.Message as Iso8583Message, node.Id, sender as ListenerPeer);
        }

        private static void DisconnectListener(object sender, SourceNode node)
        {
            Console.WriteLine($"Disconnected from {node.Name}");
            Console.WriteLine("Reconnecting...");
            (sender as ListenerPeer).Connect();
        }

        private static void SendSourceNodeResponse(ListenerPeer sourceNodePeer, Iso8583Message message)
        {
            int tries = 10;
            while (!sourceNodePeer.IsConnected && tries > 0)
            {
                Console.WriteLine("Could not connect to source node. Retrying in 5 seconds.");
                tries--;
                sourceNodePeer.Connect();
                Thread.Sleep(5000);
            }

            if (tries <= 0 && !sourceNodePeer.IsConnected)
                Console.WriteLine("Reconnection attempt failed. Could not send response to source");
            else
            {
                PeerRequest request = new PeerRequest(sourceNodePeer, message);
                request.Send();
                request.Peer.Close();
            }
        }

        private static async void ProcessTransaction(Iso8583Message message, string sourceNodeId, ListenerPeer peer)
        {
            if (message.Fields.Contains(IsoMessageFieldDefinitions.FORWARDING_INSTITUTION_CODE))
            {
                message.Fields[IsoMessageFieldDefinitions.FORWARDING_INSTITUTION_CODE].Value = ConfigurationManager.AppSettings["SwitchInstitutionCode"];
            }
            else
            {
                message.Fields.Add(IsoMessageFieldDefinitions.FORWARDING_INSTITUTION_CODE, ConfigurationManager.AppSettings["SwitchInstitutionCode"]);
            }

            Dictionary<string, object> parsedMessage = MessageToCBA(message);

            var instructions = new Dictionary<string, string>()
            {
                { "Institution Code", ConfigurationManager.AppSettings["InstitutionCode"] },
                { "Service", ConfigurationManager.AppSettings["Service"] },
                { "FlowId", ConfigurationManager.AppSettings["ProcessTransactionFlow"] }
            };
            var data = new Dictionary<string, object>()
            {
                { "IsoMessage", parsedMessage },
                { "MTI", message.MessageTypeIdentifier },
                { "SourceNodeId",  sourceNodeId }
            };
            var jsonData = new
            {
                instruction = instructions,
                data
            };

            Console.WriteLine(JsonConvert.SerializeObject(jsonData));

            HttpClient client = new HttpClient();
            client.Timeout = new TimeSpan(0, 0, 30);
            try
            {
                var content = new StringContent(JsonConvert.SerializeObject(jsonData), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(ConfigurationManager.AppSettings["WorkflowUrl"], content);
                ProcessResponseMessage(message, await response.Content.ReadAsStringAsync(), peer);
            }
            catch (Exception)
            {
                message.Fields.Add(IsoMessageFieldDefinitions.RESPONSE_CODE, IsoMessageFieldDefinitions.ResponseCodes.ISSUER_SWITCH_INOPERATIVE);
                ProcessResponseMessage(message, null, peer, true);
            }
        }

        private static void ProcessResponseMessage(Iso8583Message isoMessage, string response, ListenerPeer listener, bool isCanceled = false)
        {
            if (!isCanceled)
            {
                var message = JsonConvert.DeserializeObject<JObject>(response)["EventData"];
                var switchCheckResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(message["IsoMessage"].ToString());

                if (switchCheckResponse == null)
                    isoMessage.Fields.Add(IsoMessageFieldDefinitions.RESPONSE_CODE, IsoMessageFieldDefinitions.ResponseCodes.ERROR);
                else
                {
                    if (switchCheckResponse.Keys.Contains(IsoMessageFieldDefinitions.RESPONSE_CODE.ToString()))
                    {
                        if (isoMessage.Fields.Contains(IsoMessageFieldDefinitions.RESPONSE_CODE))
                        {
                            isoMessage.Fields[IsoMessageFieldDefinitions.RESPONSE_CODE].Value =
                                switchCheckResponse[IsoMessageFieldDefinitions.RESPONSE_CODE.ToString()].ToString();
                        }
                        else
                        {
                            isoMessage.Fields.Add(IsoMessageFieldDefinitions.RESPONSE_CODE,
                                switchCheckResponse[IsoMessageFieldDefinitions.RESPONSE_CODE.ToString()].ToString());
                        }

                    }

                    if (switchCheckResponse.Keys.Contains(IsoMessageFieldDefinitions.TRANSACTION_FEE.ToString()))
                    {
                        if (isoMessage.Fields.Contains(IsoMessageFieldDefinitions.TRANSACTION_FEE))
                        {
                            isoMessage.Fields[IsoMessageFieldDefinitions.TRANSACTION_FEE].Value =
                                  switchCheckResponse[IsoMessageFieldDefinitions.TRANSACTION_FEE.ToString()].ToString();
                        }
                        else
                        {
                            isoMessage.Fields.Add(IsoMessageFieldDefinitions.TRANSACTION_FEE,
                                  switchCheckResponse[IsoMessageFieldDefinitions.TRANSACTION_FEE.ToString()].ToString());
                        }
                    }

                    if (switchCheckResponse.Keys.Contains(IsoMessageFieldDefinitions.FROM_ACCOUNT.ToString()))
                    {
                        if (isoMessage.Fields.Contains(IsoMessageFieldDefinitions.FROM_ACCOUNT))
                        {
                            isoMessage.Fields[IsoMessageFieldDefinitions.FROM_ACCOUNT].Value =
                                  switchCheckResponse[IsoMessageFieldDefinitions.FROM_ACCOUNT.ToString()].ToString();
                        }
                        else
                        {
                            isoMessage.Fields.Add(IsoMessageFieldDefinitions.FROM_ACCOUNT,
                                  switchCheckResponse[IsoMessageFieldDefinitions.FROM_ACCOUNT.ToString()].ToString());
                        }
                    }

                    if (switchCheckResponse.Keys.Contains(IsoMessageFieldDefinitions.TO_ACCOUNT.ToString()))
                    {
                        if (isoMessage.Fields.Contains(IsoMessageFieldDefinitions.TO_ACCOUNT))
                        {
                            isoMessage.Fields[IsoMessageFieldDefinitions.TO_ACCOUNT].Value =
                                  switchCheckResponse[IsoMessageFieldDefinitions.TO_ACCOUNT.ToString()].ToString();
                        }
                        else
                        {
                            isoMessage.Fields.Add(IsoMessageFieldDefinitions.TO_ACCOUNT,
                                  switchCheckResponse[IsoMessageFieldDefinitions.TO_ACCOUNT.ToString()].ToString());
                        }
                    }

                    if (isoMessage[IsoMessageFieldDefinitions.RESPONSE_CODE].ToString().Equals(IsoMessageFieldDefinitions.ResponseCodes.APPROVED))
                    {
                        var sinkNode = JsonConvert.DeserializeObject<SourceNode>(message["SinkNode"].ToString());
                        Console.WriteLine("Sink Node IP: " + sinkNode.IPAddress);
                        Console.WriteLine("Sink Node Port: " + sinkNode.Port);

                        ClientPeer peer = new ClientPeer(sinkNode.Id, new TwoBytesNboHeaderChannel(new Iso8583Ascii1987BinaryBitmapMessageFormatter(), sinkNode.IPAddress, sinkNode.Port),
                                                            new BasicMessagesIdentifier(11, 41));

                        try
                        {
                            isoMessage = GetSinkNodeResponse(peer, isoMessage);
                        }
                        catch (Exception)
                        {
                            if (isoMessage.Fields[IsoMessageFieldDefinitions.RESPONSE_CODE].Value != null)
                            {
                                isoMessage.Fields[IsoMessageFieldDefinitions.RESPONSE_CODE].Value = IsoMessageFieldDefinitions.ResponseCodes.ISSUER_SWITCH_INOPERATIVE;
                            }
                            else
                            {
                                isoMessage.Fields.Add(IsoMessageFieldDefinitions.RESPONSE_CODE, IsoMessageFieldDefinitions.ResponseCodes.ISSUER_SWITCH_INOPERATIVE);
                            }
                        }
                    }
                    else
                    {
                        if (isoMessage.MessageTypeIdentifier == IsoMessageFieldDefinitions.MTI.AUTHORIZATION_REQUEST || isoMessage.MessageTypeIdentifier == IsoMessageFieldDefinitions.MTI.AUTHORIZATION_REQUEST_REPEAT)
                        {
                            isoMessage.MessageTypeIdentifier = IsoMessageFieldDefinitions.MTI.AUTHORIZATION_RESPONSE;
                        }
                        else if (isoMessage.MessageTypeIdentifier == IsoMessageFieldDefinitions.MTI.FINANCIAL_REQUEST || isoMessage.MessageTypeIdentifier == IsoMessageFieldDefinitions.MTI.FINANCIAL_REQUEST_REPEAT)
                        {
                            isoMessage.MessageTypeIdentifier = IsoMessageFieldDefinitions.MTI.FINANCIAL_RESPONSE;
                        }
                        else if (isoMessage.MessageTypeIdentifier == IsoMessageFieldDefinitions.MTI.REVERSAL_ADVICE || isoMessage.MessageTypeIdentifier == IsoMessageFieldDefinitions.MTI.REVERSAL_ADVICE_REPEAT)
                        {
                            isoMessage.MessageTypeIdentifier = IsoMessageFieldDefinitions.MTI.REVERSAL_RESPONSE;
                        }
                    }
                }
            }

            SendSourceNodeResponse(listener, isoMessage);
            Console.WriteLine("Transaction Finished Successfully");
        }

        private static Iso8583Message GetSinkNodeResponse(ClientPeer sinkNodePeer, Iso8583Message isoMessage)
        {
            sinkNodePeer.Connect();

            int tries = 10;
            while (!sinkNodePeer.IsConnected && tries > 0)
            {
                Console.WriteLine("Could not connect to sink node. Retrying in 5 seconds");
                tries--;
                sinkNodePeer.Connect();
                Thread.Sleep(5000);
            }

            if (tries <= 0 && !sinkNodePeer.IsConnected)
            {
                Console.WriteLine("Reconnection attempts failed. Could not connect to sink");
                if (isoMessage.Fields.Contains(IsoMessageFieldDefinitions.RESPONSE_CODE))
                    isoMessage.Fields[IsoMessageFieldDefinitions.RESPONSE_CODE].Value = IsoMessageFieldDefinitions.ResponseCodes.ISSUER_SWITCH_INOPERATIVE;
                else
                    isoMessage.Fields.Add(IsoMessageFieldDefinitions.RESPONSE_CODE, IsoMessageFieldDefinitions.ResponseCodes.ISSUER_SWITCH_INOPERATIVE);
                return isoMessage;
            }

            var request = new PeerRequest(sinkNodePeer, isoMessage);
            request.Send();
            request.WaitResponse(50000);

            var sinkNodeReponse = request.ResponseMessage as Iso8583Message;
            request.Peer.Close();

            return sinkNodeReponse;
        }

        private static Dictionary<String, Object> MessageToCBA(Iso8583Message message)
        {
            Dictionary<string, object> cbaObject = new Dictionary<string, object>();
            foreach (Field field in message.Fields)
            {
                if (field.FieldNumber != 1 && field.FieldNumber != 0)
                {
                    cbaObject.Add(field.FieldNumber.ToString(), field.Value);
                }
            }

            return cbaObject;
        }

        private static async Task<List<SourceNode>> GetSourceNodes()
        {
            var instructions = new Dictionary<String, String>()
            {
                { "Institution Code", ConfigurationManager.AppSettings["InstitutionCode"] },
                { "Service", ConfigurationManager.AppSettings["Service"] },
                { "FlowId", ConfigurationManager.AppSettings["FetchSourceNodeFlow"] }
            };

            var jsonData = new
            {
                instruction = instructions
            };

            var content = new StringContent(JsonConvert.SerializeObject(jsonData), Encoding.UTF8, "application/json");

            HttpClient httpClient = new HttpClient();
            var res = await httpClient.PostAsync(ConfigurationManager.AppSettings["WorkFlowUrl"], content);
            var nodes = JsonConvert.DeserializeObject<JObject>(await res.Content.ReadAsStringAsync())["EventData"]["SourceNodes"];

            return JsonConvert.DeserializeObject<List<SourceNode>>(nodes.ToString());

        }

        public static string CreateTrxTypeChannelFeeCombo(Scheme scheme)
        {
            var combo = "";
            var trxType = scheme.TransactionType.Code.ToString();
            var channel = scheme.Channel.Code.ToString();

            var fee = scheme.Fee.FlatAmount.ToString();
            combo += trxType + "," + channel + "," + fee;

            return combo;
        }










        public static void CreateEndPoint(string IPAddress, string port)
        {
            System.Net.IPEndPoint localEndPoint = null;
            try
            {
                //create a new client socket ...
                m_socWorker = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
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

        //                Console.WriteLine($"Now listening to
        //                tcpListener.LocalInterface = node.IPAddress;

        
        //            //                Console.WriteLine($"Now listening to {node.Name} on IP {node.IPAddress} port {node.Port}");
        //            

        //            SendReceiveOptions customSendReceiveOptions = new SendReceiveOptions<ProtoBufSerializer>();
        //            NetworkComms.AppendGlobalIncomingPacketHandler<int>("GetMessage", GetMessageRequest, customSendReceiveOptions)
        //            NetworkComms.AppendGlobalIncomingPacketHandler<string>("Message", PrintIncomingMessage);
        //            new System.Net.IPEndPoint(System.Net.IPAddress.Any, 0)
        //Start listening for incoming connections



        //We have used NetworkComms so we should ensure that we correctly call shutdown
        //            NetworkComms.Shutdown();
        //            NetworkComms.RemoveGlobalIncomingPacketHandler("Message");
        //        }

        /// <summary>
        /// Writes the provided message to the console window
        /// </summary>
        /// <param name="header">The packet header associated with the incoming message</param>
        /// <param name="connection">The connection used by the incoming message</param>
        /// <param name="message">The message to be printed to the console</param>
        private static void PrintIncomingMessage(PacketHeader header, Connection connection, string message)
        {
            //            connection.SendObject("MessageReply", new MessageObject(message));
            try
            {
                var serverInfo = connection.ConnectionInfo.LocalEndPoint.ToString();
                string serverIP = serverInfo.Split(':').First();
                int serverPort = int.Parse(serverInfo.Split(':').Last());

                if (serverInfo == "127.0.0.1:90")
                {
                    sourceNodes = Deserialize<List<SourceNode>>(message, "ArrayOfSourceNode"
                    );

                    Console.WriteLine("\nA message was received from " + serverInfo + " which said '" + message + "'.");
                    HandleNodes(sourceNodes);
                }
                else
                {

                    RoutingRules.DetermineRoute(Deserialize<Messages>(message, "Message"), combos);

                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e);

            }

        }

    }
}
