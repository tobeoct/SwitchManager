using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using ClientServerApp.Models;
using ClientServerApp.PeerConection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ClientServerApp.Helper
{
    class HelperClassManager
    {
        private static List<TransactionTypeChannelFeeCombo> combos;
        private static Listener listener;

      

        public HelperClassManager()
        {
            combos = new List<TransactionTypeChannelFeeCombo>();
            listener = new Listener();
            
        }
        public void HandleSwitchConfig(DataMessages message)

        {
            
           SwitchData.SourceNodes = message.SourceNodes;
            SwitchData.Channels = message.Channels;
            SwitchData.SinkNodes = message.SinkNodes;
            SwitchData.TransactionTypes = message.TransactionTypes;
            SwitchData.Schemes = HandleSchemes(message);
            SwitchData.Routes = message.Routes;
            HandleNodes();
        }

        public List<Scheme> HandleSchemes(DataMessages messages)

        {
            List<Scheme> schemes = messages.Schemes;
            foreach (var scheme in schemes)
            {
                if (scheme.Fee.FlatAmount == -1)
                {
                    scheme.Fee.FlatAmount = null;
                }
                if (scheme.Fee.PercentOfTrx == -1)
                {
                    scheme.Fee.PercentOfTrx = null;
                }
                if (scheme.Fee.Minimum == -1)
                {
                    scheme.Fee.Minimum = null;
                }
                if (scheme.Fee.Maximum == -1)
                {
                    scheme.Fee.Maximum = null;
                }
            }

            return schemes;
        }

        public void HandleNodes()

        {
            var sourceNodes = SwitchData.SourceNodes;
            if (sourceNodes != null)
            {

                foreach (var node in sourceNodes)
                {
                    
                    if (node.Status == "Active")
                    {

                        listener.StartListener(node);

                    }

                }
            }
            
            
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

        public T Deserialize<T>(string toDeserialize, string rootAttr)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(T), new XmlRootAttribute(rootAttr));
            using (StringReader textReader = new StringReader(toDeserialize))
            {
                return (T)xmlSerializer.Deserialize(textReader);
            }
        }

    }
}
