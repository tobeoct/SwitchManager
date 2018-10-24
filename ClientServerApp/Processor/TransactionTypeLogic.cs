using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClientServerApp.Helper;
using ClientServerApp.Models;

namespace ClientServerApp.Processor
{
    class TransactionTypeLogic
    {
        public TransactionType GetTransactionTypebyCode(string code)
        {
            List<TransactionType> transactionTypes = SwitchData.TransactionTypes;
            TransactionType transactionType = null;
          
           
            if (string.IsNullOrEmpty(code))
            {
                return null;
            }
            else
            {
                foreach (var item in transactionTypes)
                {
                    if (item.Code.ToString().PadLeft(2, '0').Equals(code))
                    {
                        transactionType = item;
                        return transactionType;
                    }
                }


            }

            return null;
            
        }
    }
}
