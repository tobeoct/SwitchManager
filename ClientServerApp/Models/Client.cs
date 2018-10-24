using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trx.Messaging.Channels;
using Trx.Messaging.FlowControl;
using Trx.Messaging.Iso8583;

namespace ClientServerApp.Models
{
    public class Client
    {
        public ClientPeer StartClient(SinkNode sinkNode)
        {
            ClientPeer clientPeer = new ClientPeer(sinkNode.Name, new TwoBytesNboHeaderChannel(
                    new Iso8583Ascii1987BinaryBitmapMessageFormatter(), sinkNode.IPAddress, Int32.Parse( sinkNode.Port)),
                    new Trx.Messaging.BasicMessagesIdentifier(11, 41));

            //clientPeer.Connect();

            clientPeer.RequestDone += new PeerRequestDoneEventHandler(Client_RequestDone);
            clientPeer.RequestCancelled += new PeerRequestCancelledEventHandler(Client_RequestCancelled);

            clientPeer.Connected += new PeerConnectedEventHandler(ClientPeerConnected);
            clientPeer.Receive += new PeerReceiveEventHandler(ClientPeerOnReceive);
            clientPeer.Disconnected += new PeerDisconnectedEventHandler(ClientPeerDisconnected);

            return clientPeer;
        }

        public static void Client_RequestDone(object sender, PeerRequestDoneEventArgs e)
        {
            Iso8583Message response = e.Request.RequestMessage as Iso8583Message;
            SourceNode source = e.Request.Payload as SourceNode;
        }

        public static void Client_RequestCancelled(object sender, PeerRequestCancelledEventArgs e)
        {
            Iso8583Message response = e.Request.RequestMessage as Iso8583Message;
            SourceNode source = e.Request.Payload as SourceNode;
        }

        private void ClientPeerDisconnected(object sender, EventArgs e)
        {
            ClientPeer client = sender as ClientPeer;
            if (client == null) return;
            //logger.Log("Disconnected from Client =/=> " + client.Name);
        }

        private void ClientPeerOnReceive(object sender, ReceiveEventArgs e)
        {
            ClientPeer clientPeer = sender as ClientPeer;
            //logger.Log("Connected to ==> " + clientPeer.Name);

            Iso8583Message receivedMsg = e.Message as Iso8583Message;

        }

        private void ClientPeerConnected(object sender, EventArgs e)
        {
            ClientPeer client = sender as ClientPeer;

            if (client == null) return;
            //logger.Log("Connected to Client ==> " + client.Name);
        }
    }
}
