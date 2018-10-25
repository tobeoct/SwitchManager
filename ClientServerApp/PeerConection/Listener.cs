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
            Console.WriteLine("Setting Response Message ...");
            if (message.IsRequest())
            {
                message.SetResponseMessageTypeIdentifier();
            }

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
                   
                    Console.WriteLine("Client is disconnected");
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
            Console.WriteLine($"Reconnected to {node.Name}");
        }

       
    }
}
