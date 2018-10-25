using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClientServerApp.Models;

namespace ClientServerApp.Helper
{
    public class SwitchData
    {
        public static List<SourceNode> SourceNodes = null;
        public static List<Scheme> Schemes = null;
        public static List<Channel> Channels = null;
        public static List<Route> Routes = null;
        public static List<TransactionType> TransactionTypes = null;
        public static List<SinkNode> SinkNodes = null;
        public static Scheme GetSchemeByRoute(Route route, string transactionTypeCode)
        {
            Scheme rtnScheme = null;
            foreach (var scheme in Schemes)
            {
                if (scheme.Route.Id == route.Id)
                {
                    if (scheme.TransactionType.Code.Equals(transactionTypeCode))
                    {
                        rtnScheme = scheme;
                    }
                    
                }
            }
            return rtnScheme;
        }
    }
}
