using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClientServerApp.Helper;
using ClientServerApp.Models;

namespace ClientServerApp.Processor
{
    class RouteLogic
    {
        public Route GetRouteByBIN(string cardBin)
        {
            List<Route> routes = SwitchData.Routes;
            Route route = null;
            if (string.IsNullOrEmpty(cardBin))
            {
                return null;
            }
            else
            {
                foreach (var item in routes)
                {
                    if (item.CardPan.ToString().Equals(cardBin))
                    {
                        route = item;
                        return route;
                    }
                }

               
            }

            return null;
        }
    }
}
