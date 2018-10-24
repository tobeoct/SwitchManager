using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClientServerApp.Helper;
using ClientServerApp.Models;

namespace ClientServerApp.Processor
{
    class ChannelLogic
    {
        public Channel GetChannelByCode(string code)
        {
            List<Channel> channels = SwitchData.Channels;
            Channel channel=null;
            foreach (var item in channels)
            {
                if (item.Code.Equals(code))
                {
                    channel = item;
                }
            }
            return channel;
        }
    }
}
