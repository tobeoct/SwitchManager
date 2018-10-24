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
using ClientServerApp.PeerConection;
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

        static void Main(string[] args)
        {
         
            EndPoints endPoints = new EndPoints(); 
            endPoints.CreateServer( "127.0.0.1", 8080);

        }
       

    }
}
