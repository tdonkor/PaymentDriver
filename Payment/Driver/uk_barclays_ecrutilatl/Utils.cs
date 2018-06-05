using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Acrelec.Mockingbird.Payment
{
    public enum TxnReceiptState : ushort
    {
        TXN_RECEIPT_DISABLED = 0,
        TXN_RECEIPT_ENABLED = 1,
    }

    public enum TransactionType : ushort
    {
        Sale = 0,
        Refund = 1,
        Void = 2,
        Duplicata = 3,
        CashAdvance = 4,
        PWCB = 5,
        PreAuth = 6,
        Completion = 7,
        VerifyCardholder = 8,
        VerifyAccount = 9,
        Reversal = 10,
    }

    public enum SignStatus : ushort
    {
        SIGN_NOT_ACCEPTED = 0,
        SIGN_CANCELLED = 1,
        SIGN_ACCEPTED = 2
    }

    public enum OfferPWCBState : ushort
    {
        PWCB_DISABLED = 0,
        PWCB_ENABLED = 1,
    }

    public enum CashbackAdditionStatus : ushort
    {
        CASHBACK_NOT_ACCEPTED = 0,
        CASHBACK_ACCEPTED = 1,
    }

    public enum TxnCancellationStatus
    {
        CANCELLATION_ERROR = 0,
        NOT_IN_TRANSACTION = 1,
        CANNOT_BE_CANCELLED = 2,
        MARKED_FOR_CANCELLATION = 3,
    }

    public struct TransactionRequest
    {
        public string MessageNumber;
        public string TxnType;
        public string Amount1;
        public string Amount2;
        public string Amount3;
        public string Amount4;
        public string Amount1Label;
        public string Amount2Label;
        public string Amount3Label;
        public string Amount4Label;
        public string ReferenceReq;
        public string TransactionId;
        public string AuthorisationCode;
        public short OfferPWCB;
    }

    public struct TransactionResponse
    {
        public string AcquirerMerchantID;
        public string MerchantAddress1;
        public string MerchantAddress2;
        public string MerchantName;
        public string AcquirerResponseCode;
        public string AuthorizationCode;
        public string CardSchemeName;
        public string Currency;
        public string TxnDateTime;
        public string EntryMethod;
        public string ExpiryDate;
        public string GemsReceiptID;
        public string MessageNumber;
        public string PAN;
        public string ReceiptNumber;
        public string ReferenceResp;
        public string TransactionStatus;
        public string AID;
        public string PANSeqNum;
        public string StartDate;
        public string TerminalId;
        public string TransactionAmount;
        public string IsDCCTxn;
        public string DCCAmount;
        public string DCCCurrency;
        public string DCCCurrencyExp;
        public string FXExponentApplied;
        public string FXRateApplied;
        public string IsLoyaltyTxn;
        public string DonationAmount;
        public string RedeemedAmount;
        public string HostMessage;
        public string TransactionType;
        public short CVM;
        public string GratuityAmount;
        public string CashAmount;
        public string TotalAmount;
        public string ICCAppFileName;
        public string ICCAppPreferredName;
        public string TransactionId;
    }

    class Utils
    {

        /// <summary>
        /// Get the name of Transaction
        /// </summary>
        public static string GetTransactionTypeString(int TxnType)
        {
            string TxnName;

            switch (TxnType)
            {
                case (int)TransactionType.Sale: TxnName = "SALE"; break;
                case (int)TransactionType.Refund: TxnName = "REFUND"; break;
                case (int)TransactionType.PWCB: TxnName = "PWCB"; break;
                case (int)TransactionType.PreAuth: TxnName = "PREAUTH"; break;
                case (int)TransactionType.Completion: TxnName = "COMPLETION"; break;
                case (int)TransactionType.CashAdvance: TxnName = "CASHADVANCE"; break;
                case (int)TransactionType.Reversal: TxnName = "REVERSAL"; break;
                case (int)TransactionType.VerifyAccount: TxnName = "VERIFY ACCOUNT"; break;
                case (int)TransactionType.Duplicata: TxnName = "DUPLICATA"; break;
                case (int)TransactionType.Void: TxnName = "VOID"; break;
                default: TxnName = "UNKNOWN"; break;
            }

            return TxnName;
        }

        /// <summary>
        /// Utility to display the diagnostics
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        public static string DisplayDiagReqOutput(int num)
        {
            string[] ReqResult = { "OK",
                                   "Unable To Connect",
                                   "Unable To Send Request",
                                   "Bad Request Format",
                                   "Reception Timeout",
                                   "Reception Error",
                                   "Bad Response Format",
                                   "Bad Response Size",
                                   "PED Not Authenticated",
                                   "No Received Data",
                                   "Unknown Value" };

            return ReqResult[num];
        }

        /// <summary>
        /// Get the Transaction Status String
        /// </summary>
        public static string DiagTxnStatus(string TxnStatus)
        {
            int ConvTxnStatus;
            string DiagTxnStatus = "";

            try { ConvTxnStatus = int.Parse(TxnStatus); }
            catch { return ""; }

            switch (ConvTxnStatus)
            {
                case 0: DiagTxnStatus = "Authorised"; break;
                case 1: DiagTxnStatus = "Not authorised"; break;
                case 2: DiagTxnStatus = "Not processed"; break;
                case 3: DiagTxnStatus = "Unable to authorise"; break;
                case 4: DiagTxnStatus = "Unable to process"; break;
                case 5: DiagTxnStatus = "Unable to connect"; break;
                case 6: DiagTxnStatus = "Void"; break;
                case 7: DiagTxnStatus = "Cancelled"; break;
                default: /* Do Nothing */ break;
            }

            return DiagTxnStatus;
        }

        /// <summary>
        /// Utility to display the terminal status
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        public static string DisplayTerminalStatus(short num)
        {
            string result = string.Empty;

            switch (num)
            {
                case 0: { result = "Unknown"; } break;
                case 1: { result = "Idle"; } break;
                case 2: { result = "Busy"; } break;
                case 3: { result = "Card Insert"; } break;
                case 4: { result = "Pin Verify"; } break;
                case 5: { result = "Authorizing"; } break;
                case 6: { result = "Completion"; } break;
                case 7: { result = "Cancelled"; } break;
                default: { result = "Error"; } break;
            }

            return result;
        }

        public static string CardEntryMethod(string num)
        {
            string result = string.Empty;

            switch (Convert.ToInt32(num))

            {
                case 0: result = "None"; break;
                case 1: result = "keyed"; break;
                case 2: result = "Swiped"; break;
                case 3: result = "Inserted"; break;
                case 4: result = "Waved"; break;
                case 5: result = "Keyed not present"; break;
            }
            return result;
        }

        /// <summary>
        /// Get the Currency Symbol String
        /// </summary>
        public static string GetCurrencySymbol(int CurrencyNum)
        {
            string CurrencySymbol = "";

            switch (CurrencyNum)
            {
                case 36: CurrencySymbol = "AUD"; break;
                case 124: CurrencySymbol = "CAD"; break;
                case 156: CurrencySymbol = "CNY"; break;
                case 203: CurrencySymbol = "CZK"; break;
                case 208: CurrencySymbol = "DKK"; break;
                case 344: CurrencySymbol = "HKD"; break;
                case 348: CurrencySymbol = "HUF"; break;
                case 376: CurrencySymbol = "ILS"; break;
                case 392: CurrencySymbol = "JPY"; break;
                case 410: CurrencySymbol = "KRW"; break;
                case 554: CurrencySymbol = "NZD"; break;
                case 578: CurrencySymbol = "NOK"; break;
                case 643: CurrencySymbol = "RUB"; break;
                case 682: CurrencySymbol = "SAR"; break;
                case 702: CurrencySymbol = "SGD"; break;
                case 710: CurrencySymbol = "ZAR"; break;
                case 752: CurrencySymbol = "SEK"; break;
                case 756: CurrencySymbol = "CHF"; break;
                case 784: CurrencySymbol = "AED"; break;
                case 826: CurrencySymbol = "£"; break;
                case 840: CurrencySymbol = "$"; break;
                case 949: CurrencySymbol = "TRY"; break;
                case 978: CurrencySymbol = "€"; break;
                case 985: CurrencySymbol = "PLN"; break;
                default: /* Do Nothing */ break;
            }

            return CurrencySymbol;
        }
    }
}
