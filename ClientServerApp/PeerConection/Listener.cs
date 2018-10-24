using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClientServerApp.Helper;
using ClientServerApp.Models;
using Trx.Messaging;
using Trx.Messaging.Channels;
using Trx.Messaging.FlowControl;
using Trx.Messaging.Iso8583;

namespace ClientServerApp.PeerConection
{
    public class Listener
    {
        private static Processor.Processor processor;
        public Listener()
        {
            processor=new Processor.Processor();
        }
        public  void StartListener(SourceNode node)
        {
            TcpListener tcpListener = new TcpListener(node.Port);
            tcpListener.LocalInterface = node.IPAddress;
            ListenerPeer listener = new ListenerPeer(node.Id.ToString(),
                new TwoBytesNboHeaderChannel(new Iso8583Ascii1987BinaryBitmapMessageFormatter()),
                new BasicMessagesIdentifier(11, 41),
                tcpListener);
            //            ListenerPeer
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

        private static void ReceiveListener(object sender, ReceiveEventArgs e, SourceNode node)
        {
            Console.WriteLine($"Received message from {node.Name} node");
//            Console.WriteLine("Message : "+ e.Message );
           var msg = processor.ProcessMessage(e.Message as Iso8583Message,node);
//            Console.WriteLine(msg.Fields[39].Value.ToString());
            msg = RouteResponseToSource(sender as ListenerPeer, msg);
            if (msg.Fields[39].Value.ToString().Equals("00"))
            {
                Console.WriteLine("Transaction Complete");

            }
            else
            {
                Console.WriteLine("Transaction Incomplete");
            }
//            processor.ProcessTransaction(e.Message as Iso8583Message, node.Id, sender as ListenerPeer, schemes);
        }
        private static Iso8583Message SetResponseMessage(Iso8583Message message, string responseCode)
        {
            Console.WriteLine("Setting Resp Msg ...");
            message.SetResponseMessageTypeIdentifier();
            message.Fields.Add(39, responseCode);
            return message;
        }

        private static Iso8583Message RouteResponseToSource(ListenerPeer listenerPeer, Iso8583Message message)
        {
            bool needReversal = false;
            int maxNumberOfEntries = 3;
            Message response = null;
            int serverTimeOut = 60000;

            PeerRequest request = null;
            try
            {
                if (listenerPeer.IsConnected)
                {
                    request = new PeerRequest(listenerPeer, message);
                    request.Send();
                    request.WaitResponse(serverTimeOut);
                    if (request.Expired)
                    {
                        //logger.Log("Connection timeout.");
                        needReversal = true;
                        return SetResponseMessage(message, "68"); //Response received too late

                    }

                    if (request != null)
                    {
                        response = request.ResponseMessage;
                        //logger.Log("Message Recieved From FEP");

                    }

                    listenerPeer.Close();
                    //request.MarkAsExpired();   //uncomment to test timeout
                    return response as Iso8583Message;

                }
                else
                {
                    //logger.Log("Could not connect to Sink Node");
                    //clientPeer.Close();
                    Console.WriteLine("Client Peer is not Connected");
                    return SetResponseMessage(message, "91");
                }

                //clientPeer.Close();
            }
            catch (Exception e)
            {
                //logger.Log("An error occured " + e.Message);
                return SetResponseMessage(message, "06");
            }
        }

        private static void DisconnectListener(object sender, SourceNode node)
        {
            Console.WriteLine($"Disconnected from {node.Name}");
            Console.WriteLine("Reconnecting...");
            (sender as ListenerPeer).Connect();
        }

        //        public void StartListener(SourceNode sourceNode)
        //        {
        //            TcpListener tcpListener = new TcpListener(sourceNode.Port);
        //            tcpListener.LocalInterface = sourceNode.IPAddress;
        //            tcpListener.Start();
        //
        //            ListenerPeer listenerPeer = new ListenerPeer(sourceNode.Id.ToString(), new TwoBytesNboHeaderChannel
        //                (new Iso8583Ascii1987BinaryBitmapMessageFormatter(), sourceNode.IPAddress, sourceNode.Port),
        //                new BasicMessagesIdentifier(11, 41), tcpListener);
        //
        //            //logger.Log("Source :" + sourceNode.Name + " now listening at: " + sourceNode.IPAddress + " on: " + sourceNode.Port);
        //
        //            listenerPeer.Connected += new PeerConnectedEventHandler(ListenerPeerConnected);
        //            listenerPeer.Receive += new PeerReceiveEventHandler(ListenerPeerReceive);
        //            listenerPeer.Disconnected += new PeerDisconnectedEventHandler(ListenerPeerDisconnected);
        //        }
        //
        //        private /*static*/ void ListenerPeerReceive(object sender, ReceiveEventArgs e)
        //        {
        //
        //            ListenerPeer sourcePeer = sender as ListenerPeer;
        //            //logger.Log("Listener Peer now Recieving....");
        //
        //            //Get the ISO message
        //            Iso8583Message receivedMessage = e.Message as Iso8583Message;
        //
        //            new TransactionLogLogic().LogTransaction(receivedMessage);
        //
        //            //receivedMessage.
        //            //string cardpan = receivedMessage.Fields[2].Value.ToString();
        //
        //            if (receivedMessage == null) return;
        //
        //            int sourceID = Convert.ToInt32(sourcePeer.Name);
        //
        //            Iso8583Message responseMessage = new Processor().ProcessMessage(receivedMessage, sourceID);
        //
        //            sourcePeer.Send(responseMessage);
        //            sourcePeer.Close();
        //            sourcePeer.Dispose();
        //        }
        //        private void ListenerPeerConnected(object sender, EventArgs e)
        //        {
        //            ListenerPeer listenerPeer = sender as ListenerPeer;
        //            if (listenerPeer == null) return;
        //            //HOW CAN LISTENER PEER BE CONNECTED TO LISTENER??? s
        //            //logger.Log("Listener Peer connected to " + listenerPeer.Name);
        //        }
        //        private void ListenerPeerDisconnected(object sender, EventArgs e)
        //        {
        //            ListenerPeer listenerPeer = sender as ListenerPeer;
        //            if (listenerPeer == null) return;
        //            //logger.Log("Listener Peer disconnected from " + listenerPeer.Name);
        //
        //            SourceNode sourceNode = new EntityLogic<SourceNode>().GetByID(Convert.ToInt32(listenerPeer.Name));
        //            StartListener(sourceNode);
        //        }
    }
}
