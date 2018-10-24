using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ClientServerApp.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SwitchConsole.Models;
using Trx.Messaging;
using Trx.Messaging.Channels;
using Trx.Messaging.FlowControl;
using Trx.Messaging.Iso8583;

namespace ClientServerApp.Helper
{
    public class Procesr
    {
        public List<Scheme> schemeList;

        public Procesr()
        {
            schemeList = new List<Scheme>();
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

        public async void ProcessTransaction(Iso8583Message message, string sourceNodeId, ListenerPeer peer,
            List<Scheme> schemes)
        {
            schemeList = schemes;
            if (message.Fields.Contains(IsoMessageFieldDefinitions.FORWARDING_INSTITUTION_CODE))
            {
                message.Fields[IsoMessageFieldDefinitions.FORWARDING_INSTITUTION_CODE].Value =
                    ConfigurationManager.AppSettings["SwitchInstitutionCode"];
            }
            else
            {
                message.Fields.Add(IsoMessageFieldDefinitions.FORWARDING_INSTITUTION_CODE,
                    ConfigurationManager.AppSettings["SwitchInstitutionCode"]);
            }

            Dictionary<string, object> parsedMessage = MessageToCBA(message);

            var instructions = new Dictionary<string, string>()
            {
                {"Institution Code", ConfigurationManager.AppSettings["InstitutionCode"]},
                {"Service", ConfigurationManager.AppSettings["Service"]},
                {"FlowId", ConfigurationManager.AppSettings["ProcessTransactionFlow"]}
            };
            var data = new Dictionary<string, object>()
            {
                {"IsoMessage", parsedMessage},
                {"MTI", message.MessageTypeIdentifier},
                {"SourceNodeId", sourceNodeId}
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
                var content = new StringContent(JsonConvert.SerializeObject(jsonData), Encoding.UTF8,
                    "application/json");
                var response = await client.PostAsync(ConfigurationManager.AppSettings["WorkflowUrl"], content);
                ProcessResponseMessage(message, await response.Content.ReadAsStringAsync(), peer);
            }
            catch (Exception)
            {
                message.Fields.Add(IsoMessageFieldDefinitions.RESPONSE_CODE,
                    IsoMessageFieldDefinitions.ResponseCodes.ISSUER_SWITCH_INOPERATIVE);
                ProcessResponseMessage(message, null, peer, true);
            }
        }

        public void RouteToDestination(Iso8583Message message, SinkNode sinknode)
        {

            int maxNumberOfEntries = 3;
            int serverTimeOut = 60000;


            ClientPeer clientPeer = new Client().StartClient(sinknode);

            int retries = 0;
            while (retries < maxNumberOfEntries)
            {
                if (clientPeer.IsConnected)
                {
                    break;
                }
                else
                {
                    //clientPeer.Close();
                    retries++;
                    clientPeer.Connect();
                }

                Thread.Sleep(5000);


            }

            PeerRequest request = null;
            if (clientPeer.IsConnected)
            {
                request = new PeerRequest(clientPeer, message);
                request.Send();
                request.WaitResponse(serverTimeOut);
                //request.MarkAsExpired();   //uncomment to test timeout

                if (request.Expired)
                {
                    //logger.Log("Connection timeout.");
                    
                    Console.WriteLine("Response received too late"); //Response received too late
                }
                //                    if (request != null)
                //                    {
                //                        response = request.ResponseMessage;
                //                        //logger.Log("Message Recieved From FEP");
                //
                //                    }

                clientPeer.Close();
                //                    return response as Iso8583Message;

            }
            else
            {
                //logger.Log("Could not connect to Sink Node");
                //clientPeer.Close();
                Console.WriteLine("Client Peer is not Connected");
              
            }

            //clientPeer.Close();
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

                        ClientPeer peer = new ClientPeer(sinkNode.Id.ToString(), new TwoBytesNboHeaderChannel(new Iso8583Ascii1987BinaryBitmapMessageFormatter(), sinkNode.IPAddress, sinkNode.Port),
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

    }
}
