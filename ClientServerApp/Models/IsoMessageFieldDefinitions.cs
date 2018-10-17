using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwitchConsole.Models
{
    class IsoMessageFieldDefinitions
    {
        public class ResponseCodes
        {
            public static string APPROVED = "00";

            public static string TERMINAL_NOT_ALLOWED = "58";

            public static string ISSUER_SWITCH_INOPERATIVE = "91";

            public static string INVALID_TRANSACTION = "12";

            public static string INVALID_AMOUNT = "13";

            public static string CARD_EXPIRED = "33";

            public static string NO_CARD_RECORD = "56";

            public static string BAD_TRANSACTION_FEE = "23";

            public static string ERROR = "06";
        }

        public class TransactionTypes
        {
            public static string BALANCE_INQUIRY = "31";
        }

        public static int RESPONSE_CODE = 39;

        public static int PROCCESSING_CODE = 3;

        public static int TRANSACTION_AMOUNT = 4;

        public static int CARD_EXPIRATION_DATE = 14;

        public static int CARD_PAN = 2;

        public static int TRACE_AUDIT_NUMBER = 11;

        public static int TRANMISSION_DATE_TIME = 7;

        public static int ACQUIRER_INSTITUTION_CODE = 32;

        public static int FORWARDING_INSTITUTION_CODE = 33;

        public static int ORIGINAL_DATA_ELEMENTS = 90;

        public static int TRANSACTION_FEE = 28;

        public static int FROM_ACCOUNT = 102;

        public static int TO_ACCOUNT = 103;

        public class MTI
        {
            public static int AUTHORIZATION_REQUEST = 0100;

            public static int AUTHORIZATION_REQUEST_REPEAT = 0101;

            public static int FINANCIAL_REQUEST = 0200;

            public static int FINANCIAL_REQUEST_REPEAT = 0201;

            public static int REVERSAL_ADVICE = 0420;

            public static int REVERSAL_ADVICE_REPEAT = 0421;

            public static int AUTHORIZATION_RESPONSE = 0110;

            public static int FINANCIAL_RESPONSE = 0210;

            public static int REVERSAL_RESPONSE = 0430;
        }
    }
}
