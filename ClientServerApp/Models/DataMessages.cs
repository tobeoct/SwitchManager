using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientServerApp.Models
{
    public class DataMessages
    {

        public List<SourceNode> SourceNodes { get; set; }
        public List<Scheme> Schemes { get; set; }
        public List<Route> Routes { get; set; }
        public List<TransactionType> TransactionTypes { get; set; }
        public List<SinkNode> SinkNodes { get; set; }
        public List<Channel> Channels { get; set; }
    }
}
