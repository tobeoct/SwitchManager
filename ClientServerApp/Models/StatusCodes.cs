using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ClientServerApp.Models
{
    public class StatusCodes
    {
        public static string UPDATED = "Updated Successfully";
        public static string DELETED = "Deleted Successfully";
        public static string CREATED = "Created Successfully";
        public static string ACTIVE = "Active";
        public static string INACTIVE = "In-active";
        public static string ERROR_DOESNT_EXIST = "Item doesn't exist";
        public static string NAME_ALREADY_EXIST = "Name already exist";
    }

    public class TransactionTypeCodes
    {
        public static int WITHDRAWAL = 01;
        public static int PAYMENT = 02;

    }

    public class ChannelCodes
    {
        public static int ATM = 10;
        public static int POS = 20;
        public static int WEB = 30;
    }

   

    public class TransactionTypeChannelFeeCombo
    {
        public string Combo { get; set; }
        public SinkNode SinkNode { get; set; }
    }

}