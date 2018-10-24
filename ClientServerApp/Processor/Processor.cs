using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ClientServerApp.Helper;
using ClientServerApp.Models;
using Trx.Messaging;
using Trx.Messaging.FlowControl;
using Trx.Messaging.Iso8583;

namespace ClientServerApp.Processor
{
    public class Processor
    {
        public Processor()
        {

        }
        public Iso8583Message ProcessMessage(Iso8583Message message, SourceNode sourceNode)
        {
            Console.WriteLine("Processing ...");
            if (message.MessageTypeIdentifier != 420)
            {
                message = AddOriginalDataElement(message);
            }
            //            SourceNode sourceNode = new EntityLogic<SourceNode>().GetByID(sourceID);
            bool returnMsg;
            message = CheckMessageType(message, sourceNode, out returnMsg);

            if (returnMsg == true)
            {
                return message;
            }

            Iso8583Message responseMessage;
            string expiryDate = message.Fields[14].Value.ToString();
            DateTime cardExpiry = ParseDate(expiryDate);

            if (cardExpiry < DateTime.Now)
            {
                //expired card
                responseMessage = SetResponseMessage(message, "54");
                new TransactionLogProcessing().LogTransaction(responseMessage);
                return responseMessage;
            }

            Console.WriteLine("Getting TrxCode ...");
            string transactionTypeCode = message.Fields[3].Value.ToString().Substring(0, 2);

            Console.WriteLine("Getting Amount ...");
            double amount = ConvertIsoAmountToDouble(message.Fields[4].Value.ToString());
            if (transactionTypeCode != "31" && amount <= 0)
            {
                responseMessage = SetResponseMessage(message, "13");
                new TransactionLogProcessing().LogTransaction(responseMessage);
                return responseMessage;
            }

            // GETTING ROUTE
    
            Console.WriteLine("Getting BIN ...");
            string cardBIN = message.Fields[2].Value.ToString().Substring(0, 6);

            Console.WriteLine("Getting Route ...");
            Route route = new RouteLogic().GetRouteByBIN(cardBIN);
           
            if (route == null)
            {
                responseMessage = SetResponseMessage(message, "15");
                new TransactionLogProcessing().LogTransaction(responseMessage);
                return responseMessage;
            }

            if (route.SinkNode == null || route.SinkNode.Status == "In-active")
            {
                Console.WriteLine("Sink Node is null");
                responseMessage = SetResponseMessage(message, "91");
                new TransactionLogProcessing().LogTransaction(responseMessage);
                return responseMessage;
            }

            //GETTING SCHEME

            Scheme scheme;

            try

            {
                Console.WriteLine("Getting Scheme ...");
                scheme = SwitchData.GetSchemeByRoute(route);

            }
            catch (Exception e)
            {
                responseMessage = SetResponseMessage(message, "06");
                new TransactionLogProcessing().LogTransaction(responseMessage);
                return responseMessage;
            }

            if (scheme == null)
            {
                responseMessage = SetResponseMessage(message, "58");
                new TransactionLogProcessing().LogTransaction(responseMessage);
                //new TransactionLogLogic().LogMessage(responseMessage);
                return responseMessage;
            }

            Console.WriteLine("Getting TrxType ...");
            TransactionType transactionType = new TransactionTypeLogic().GetTransactionTypebyCode(transactionTypeCode);

            Console.WriteLine("Getting Channel ...");
            string channelCode = message.Fields[41].Value.ToString().Substring(0, 1);

            Channel channel = new ChannelLogic().GetChannelByCode(channelCode);

            Fee fee = GetFee(transactionType, channel, scheme);

            if (fee == null)
            {
                responseMessage = SetResponseMessage(message, "58");
                new TransactionLogProcessing().LogTransaction(responseMessage);
                return responseMessage;
            }

            double? fees = CalculateFee(fee, amount);

            message = SetFee(message, fees);

            //NOW WE ARE DONE WITH CHECKS, WE CAN SEND

            bool needReversal = false;
            Console.WriteLine("Routing To Destination ...");
            responseMessage = RouteToDestination(message, route.SinkNode, out needReversal);


            return responseMessage;
        }

       
        private Iso8583Message CheckMessageType(Iso8583Message originalMessage, SourceNode sourceNode, out bool returnMessage)
        {
            Console.WriteLine("Checking Message Type ...");
            if (originalMessage.MessageTypeIdentifier == 421 || originalMessage.MessageTypeIdentifier == 420)
            {
                bool doReversal = false; //doReversal
                Iso8583Message reversalIsoMsg = null;
                reversalIsoMsg = PerformReversal(originalMessage, out doReversal);

                if (!doReversal)
                {
                    //Reversal already done somewhere else or something went wrong
                    //LogTransaction(reversalIsoMsg);
                    //returnMessage = true to return message in the ProcessMessage method
                    //if returnMessage is false, continue using the message in the ProcessMessage method
                    returnMessage = true;
                    return reversalIsoMsg;
                }
                //do reversal
                //change message to reversal message
                originalMessage = reversalIsoMsg;
                //LogTransaction(message, sourceNode);
                returnMessage = true;
            }

            returnMessage = false;
            return originalMessage;
        }

        public Iso8583Message PerformReversal(Iso8583Message message, out bool doReversal)
        {
            doReversal = true;
            bool needReversal = false;
            Console.WriteLine("Getting BIN ...");
            string cardBIN = message.Fields[2].Value.ToString().Substring(0, 6);

            Console.WriteLine("Getting Route ...");
            Route route = new RouteLogic().GetRouteByBIN(cardBIN);
            if (route != null)
            {
                if (route.SinkNode != null)
                {
                    RouteToDestination(message, route.SinkNode, out needReversal);
                }
            }
           
            return message;
        }

        private Iso8583Message AddOriginalDataElement(Iso8583Message message)
        {
            Console.WriteLine("Adding Element ...");
            DateTime transmissionDate = DateTime.UtcNow;
            string transactionDate = string.Format("{0}{1}",
                    string.Format("{0:00}{1:00}", transmissionDate.Month, transmissionDate.Day),
                    string.Format("{0:00}{1:00}{2:00}", transmissionDate.Hour,
                    transmissionDate.Minute, transmissionDate.Second));

            //string originalDataElement = string.Format("{0}{1}{2}", message.MessageTypeIdentifier.ToString(),
            //     message.Fields[11].ToString(), transactionDate);
            string originalDataElement = string.Format("{0:0000}{1:000000}{2:MMddHHmmss}{3:00000000000}{4:00000000000}", message.MessageTypeIdentifier.ToString().PadLeft(4, '0'),
                 message.Fields[11].ToString().PadLeft(6, '0'), transactionDate, message.Fields[32].ToString().PadLeft(11, '0'), message.Fields[33].ToString().PadLeft(11, '0'));
            message.Fields.Add(90, originalDataElement);

            return message;
        }

        private DateTime ParseDate(string date)
        {
            string year = date.Substring(0, 2);
            string month = date.Substring(2, 2);

            string expiry = month + "-" + "30" + "-" + year;
            //MM-DD-YY
            DateTime cardExpiryDate;

            if (DateTime.TryParse(expiry, out cardExpiryDate))
            {
                //is a date
                return cardExpiryDate;
            }
            else
            {
                //not a valid date
                return DateTime.Now;
            }
        }

        private Iso8583Message SetResponseMessage(Iso8583Message message, string responseCode)
        {
            Console.WriteLine("Setting Resp Msg ...");
            message.SetResponseMessageTypeIdentifier();
            message.Fields.Add(39, responseCode);
            return message;
        }

        private double ConvertIsoAmountToDouble(string amountIsoFormat)
        {
            double amount = Convert.ToDouble(amountIsoFormat) / 100;
            return amount;
        }

        private Fee GetFee(TransactionType transactionType, Channel channel, Scheme scheme)
        {
            if (transactionType != null && channel != null)
            {
                var transactionTypeCode = transactionType.Code;
                var channelCode = channel.Code;

                if (transactionTypeCode.ToString().Equals(scheme.TransactionType.Code))
                {
                    if (channelCode.ToString().Equals(scheme.Channel.Code))
                    {
                        Fee fee = scheme.Fee;
                        return fee;
                    }
                }
            }

            return null;
        }
        private double? CalculateFee(Fee fee, double amount)
        {
            if (fee.FlatAmount != null && fee.PercentOfTrx == null)
            {
                //percentage used
                return fee.FlatAmount;
            }
            else if (fee.FlatAmount == null && fee.PercentOfTrx != null)
            {
                //flat amount used
                double? fees = (fee.PercentOfTrx / 10) * amount;

                if (fees < fee.Minimum)
                {
                    fees = fee.Minimum;
                }
                else if (fees > fee.Maximum)
                {
                    fees = fee.Maximum;
                }
                else
                {
                    fees = 0;
                }

                return fees;
            }
            else
            {
                //error, ambiguos stuff
                return 0;
            }
        }

        private Iso8583Message SetFee(Iso8583Message message, double? fee)
        {
            string feeAmount = ConvertFeeToISOFormat(fee);
            message.Fields.Add(28, feeAmount);
            return message;
        }

        private string ConvertFeeToISOFormat(double? fee)
        {
            //takes in 40naira C0000004000
            double? feeInMinorDenomination = fee * 100; //in Kobo

            StringBuilder feeStringBuilder = new StringBuilder(Convert.ToInt32(feeInMinorDenomination).ToString());
            string padded = feeStringBuilder.ToString().PadLeft(8, '0');
            feeStringBuilder.Replace(feeStringBuilder.ToString(), padded);
            feeStringBuilder.Insert(0, 'C');

            return feeStringBuilder.ToString();
        }

        private Iso8583Message RouteToDestination(Iso8583Message message, SinkNode sinkNode, out bool needReversal)
        {
            Message response = null;
            needReversal = false;
            try
            {
                if (message == null)
                {
                    return SetResponseMessage(message, "20");
                }
                if (sinkNode == null)
                {
                    Console.WriteLine("Sink Node is null");
                    return SetResponseMessage(message, "91");
                }
                if (sinkNode.Status == "In-active")
                {
                    Console.WriteLine("Sink Node is Inactive");
                    return SetResponseMessage(message, "91");
                }
                int maxNumberOfEntries = 3;
                int serverTimeOut = 60000;


                ClientPeer clientPeer = new Client().StartClient(sinkNode);

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
                        needReversal = true;
                        return SetResponseMessage(message, "68");  //Response received too late
                    }
                    if (request != null)
                    {
                        response = request.ResponseMessage;
                        request = new PeerRequest(clientPeer, SetResponseMessage(message, "00"));
                        request.Send();
                        //logger.Log("Message Recieved From FEP");

                    }

                    clientPeer.Close();
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
    }
}

