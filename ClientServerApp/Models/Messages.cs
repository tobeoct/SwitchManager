using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientServerApp.Models
{
   public class Messages
    {
        public int TransactionType { get; set; }
        public int Channel { get; set; }
        public double Amount { get; set; }
    }
}
