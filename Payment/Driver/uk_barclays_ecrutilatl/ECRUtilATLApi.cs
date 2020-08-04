using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Acrelec.Library.Logger;
using Acrelec.Mockingbird.Payment.Configuration;
using Acrelec.Mockingbird.Payment.Contracts;
using ECRUtilATLLib;


namespace Acrelec.Mockingbird.Payment
{
    public class ECRUtilATLApi: IDisposable
    {
        TerminalIPAddress termimalIPAddress;
        StatusClass termimalStatus;
        InitTxnReceiptPrint initTxnReceiptPrint;
        TransactionClass transaction;
        TimeDateClass timeDate;
        TransactionResponse transactionResponse;
        SignatureClass checkSignature;
        VoiceReferralClass checkVoiceReferral;
        Thread SignatureVerificationThread;
        Thread VoiceReferralThread;
        SettlementClass getSettlement;
        SettlementRequest settlementRequest;


        /// <summary>
        /// Constructor initialise All the objects needed
        /// </summary>
        public ECRUtilATLApi()
        {
            transaction = new TransactionClass();
        }

        public void Dispose()
        {
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool FreeLibrary(IntPtr hModule);

        /// <summary>
        /// Check the prerequistes have been set:
        /// Ip Address is called
        /// Status is at IDLE
        /// Disable the printing to print from the Transaction response
        /// </summary>
        /// <returns></returns>
        public ECRUtilATLErrMsg Connect(string ipAddress)
        {
            //set static IP address
            termimalIPAddress = new TerminalIPAddress();
            termimalIPAddress.IPAddressIn = ipAddress;
            termimalIPAddress.SetIPAddress();

            // check the status is at IDLE
            string status = string.Empty;
            termimalStatus = new StatusClass();
            termimalStatus.GetTerminalState();
            Log.Info($"Check Terminal at Idle: {Utils.DisplayTerminalStatus(Convert.ToInt16(termimalStatus.StateOut))}");


            // disable the receipt Printing
            initTxnReceiptPrint = new InitTxnReceiptPrint();
            initTxnReceiptPrint.StatusIn = (short)TxnReceiptState.TXN_RECEIPT_DISABLED;
            initTxnReceiptPrint.SetTxnReceiptPrintStatus();

            //check printing disabled
            if ((int.Parse(initTxnReceiptPrint.DiagRequestOut) == 0))
                Log.Info("apiInitTxnReceiptPrint OFF");
            else
                Log.Info("apiInitTxnReceiptPrint ON");

            //Set the time 
            timeDate = new TimeDateClass();
            timeDate.YearIn = DateTime.Now.Year.ToString();
            timeDate.MonthIn = DateTime.Now.Month.ToString();
            timeDate.DayIn = DateTime.Now.Day.ToString();
            timeDate.HourIn = DateTime.Now.Hour.ToString();
            timeDate.MinuteIn = DateTime.Now.Minute.ToString();
            timeDate.SecondIn = DateTime.Now.Second.ToString();

            timeDate.SetTimeDate();

            //check the connection result 
            return (ECRUtilATLErrMsg)Convert.ToInt32(termimalIPAddress.DiagRequestOut);
        }

       

        /// <summary>
        /// Disconect the transaction
        /// </summary>
        public ECRUtilATLErrMsg Disconnect()
        {
            Log.Info("Disconnecting...Reset the transaction");
            transaction.Reset();

            ECRUtilATLErrMsg disconnResult = ECRUtilATLErrMsg.UknownValue;

            if ((ECRUtilATLErrMsg)Convert.ToInt32(transactionResponse.DiagRequestOut) == ECRUtilATLErrMsg.OK)
                disconnResult = ECRUtilATLErrMsg.OK;
            else
                disconnResult = ECRUtilATLErrMsg.UknownValue;

            return disconnResult;

        }

        /// <summary>
        /// The transaction Payment
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public ECRUtilATLErrMsg Pay(int amount, out TransactionResponse result)
        {
            int intAmount;
            Log.Info($"Executing payment - Amount: {amount/100.0}");

            //check amount is valid
            intAmount = Utils.GetNumericAmountValue(amount);

            if (intAmount == 0)
                throw new Exception("Error in input");

            // Transaction Sale details
            //
            DoTransaction(amount, TransactionType.Sale.ToString());

            result = PopulateResponse(transaction);
            return (ECRUtilATLErrMsg)Convert.ToInt32(transaction.DiagRequestOut);
        }

        /// <summary>
        /// Payment Reversal
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public ECRUtilATLErrMsg Reverse(int amount, out TransactionResponse result)
        {
            int intAmount;
            Log.Info($"Executing Reversal - Amount: {amount/100.0}");

            //check amount is valid
            intAmount = Utils.GetNumericAmountValue(amount);

            if (intAmount == 0)
                throw new Exception("Error in input");
           
            DoTransaction(amount, TransactionType.Reversal.ToString());
            result = PopulateResponse(transaction);

            return (ECRUtilATLErrMsg)Convert.ToInt32(transaction.DiagRequestOut);

        }

        /// <summary>
        /// End of day report
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public SettlementClass EndOfDayReport()
        {
            Log.Info("Printing end of day report...");

            // Get Acquirer List
            // getAcquirerList.Launch();

            getSettlement = new SettlementClass();
            settlementRequest = new SettlementRequest();

            // do the settlement
            getSettlement.AcquirerIndexIn = settlementRequest.AcquirerIndex;
            getSettlement.SettlementParamIn = (short)settlementRequest.SettlementParameter;
            getSettlement.DoSettlement();

            if ((ECRUtilATLErrMsg)(Convert.ToInt32(getSettlement.DiagRequestOut)) == ECRUtilATLErrMsg.OK)
                return getSettlement;
            else return null;
        }
 

        /// <summary>
        ///  Do the transaction
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="transactionType"></param>
        public void DoTransaction(int amount, string transactionType)
        {
            Random randomNum = new Random();
            Log.Info($"Selected Transaction type:{Utils.GetSelectedTransaction(transactionType).ToString()}");
          
            transaction.MessageNumberIn = randomNum.Next(100).ToString();
            transaction.TransactionTypeIn = Utils.GetSelectedTransaction(transactionType).ToString();
            transaction.Amount1In = amount.ToString().PadLeft(12, '0');
            transaction.Amount1LabelIn = "Amount1";
            transaction.Amount2In = "";
            transaction.Amount2LabelIn = "";
            transaction.Amount3In = "";
            transaction.Amount3LabelIn = "";
            transaction.Amount4In = "";
            transaction.Amount4LabelIn = "";
            transaction.ReferenceIn = "";
            transaction.TransactionIDIn = "";
            transaction.AuthorizationCodeIn = "";
            transaction.OfferPWCBIn = (short)OfferPWCBState.PWCB_DISABLED;

            //set signature verification
            SignatureVerificationThread = new Thread(SignatureVerification);
            SignatureVerificationThread.Start();

            VoiceReferralThread = new Thread(VoiceReferralAuthorisation);
            VoiceReferralThread.Start();

            Log.Info($"Transaction {transaction.TransactionTypeIn} launched...");
            transaction.Launch();

            // Trying to abort the signature verification thread if it is alive
            try { SignatureVerificationThread.Abort(); }
            catch (Exception ThreadException) { Log.Error(ThreadException.StackTrace); }
            SignatureVerificationThread = null;

            // Trying to abort the voice referral authorisation thread if it is alive
            try { VoiceReferralThread.Abort(); }
            catch (Exception ThreadException) { Log.Error(ThreadException.StackTrace); }
            VoiceReferralThread = null;

            Log.Info($"Transaction Card scheme out: {transaction.CardSchemeNameOut}");
            Log.Info($"Transaction CVM out: {Utils.CardVerification(transaction.CVMOut)}");
            Log.Info($"Transaction Entry Method out:{Utils.CardEntryMethod(transaction.EntryMethodOut)}");
            Log.Info($"Transaction Total amount: £{Convert.ToSingle(transaction.TotalTransactionAmountOut)/100.0}");
            Log.Info($"Transaction Terminal Identity out: {transaction.TerminalIdentityOut}");           
        }

        /// <summary>
        /// Verify Signature
        /// </summary>
        public void SignatureVerification()
        {
            //local variables
            int ret = 0;
            string CheckSignatureStatus = string.Empty;

            checkSignature = new SignatureClass();
            checkSignature.CheckSignReq();

            Log.Info("Running Check Signature");


            ret = Int32.Parse(checkSignature.DiagRequestOut);
            Log.Info($" checkSignature = {ret}");

            switch (ret)
            {
                case 0: CheckSignatureStatus = "Signature In Progress"; break; // RET_OK
                case 1: CheckSignatureStatus = "Server Down"; break;           // RET_SIGN_SERVER_DOWN
                case 2: CheckSignatureStatus = "Timeout"; break;               // RET_TIMEOUT
                case 3: CheckSignatureStatus = "Bad Request Size"; break;      // RET_BAD_REQUEST_SIZE
                case 4: CheckSignatureStatus = "Bad Request Format"; break;    // RET_BAD_REQUEST_FORMAT
                case 8: CheckSignatureStatus = "Ped Not Auth"; break;          // RET_PED_NOT_AUTHENTICATED
                default:CheckSignatureStatus = "Unknown Status"; break;       // Unknown Status
            }

            if (ret != 0) //Error Case
            {
                Log.Error("Error in CheckSignature return ");
            }
            else
            {
                //set the signature Status
                checkSignature.SignatureStatusIn = 2; //assign
                checkSignature.SetSignStatus();
                Log.Info($" SetSignStatus = {checkSignature.DiagRequestOut}");
            }            
        }


        public void VoiceReferralAuthorisation()
        {

            checkVoiceReferral = new VoiceReferralClass();
            checkVoiceReferral.CheckVoiceReferralReq();
            Log.Info($"checkVoiceReferral out: {checkVoiceReferral.DiagRequestOut}");

            //decline the voice referral 
            checkVoiceReferral.AuthorisationStatusIn = 1; // Decline
            checkVoiceReferral.AuthorisationCodeIn = "";
            checkVoiceReferral.SetAuthorisation();
            checkVoiceReferral = null;
        }

        /// <summary>
        /// Populate the transaction response Object
        /// </summary>
        /// <param name="transaction"></param>
        /// <returns>transactionResponse</returns>
        private TransactionResponse PopulateResponse(TransactionClass transaction)
        {
            Log.Info("Populating Transaction Response");

            transactionResponse.AcquirerMerchantID = transaction.AcquirerMerchantIDOut;
            transactionResponse.MerchantAddress1 = transaction.MerchantAddress1Out;
            transactionResponse.MerchantAddress2 = transaction.MerchantAddress2Out;
            transactionResponse.MerchantName = transaction.MerchantNameOut;
            transactionResponse.AcquirerResponseCode = transaction.AcquirerResponseCodeOut;
            transactionResponse.AuthorizationCode = transaction.AuthorizationCodeOut;
            transactionResponse.CardSchemeName = transaction.CardSchemeNameOut;
            transactionResponse.Currency = transaction.CurrencyOut;
            transactionResponse.TxnDateTime = transaction.DateTimeOut;
            transactionResponse.EntryMethod = transaction.EntryMethodOut;
            transactionResponse.ExpiryDate = transaction.ExpiryDateOut;
            transactionResponse.GemsReceiptID = transaction.GemsReceiptIDOut;
            transactionResponse.MessageNumber = transaction.MessageNumberOut;
            transactionResponse.PAN = transaction.PANOut;
            transactionResponse.ReceiptNumber = transaction.ReceiptNumberOut;
            transactionResponse.ReferenceResp = transaction.ReferenceOut;
            transactionResponse.AID = transaction.AIDOut;
            transactionResponse.PANSeqNum = transaction.PANSequenceNumberOut;
            transactionResponse.StartDate = transaction.StartDateOut;
            transactionResponse.TerminalId = transaction.TerminalIdentityOut;
            transactionResponse.TransactionAmount = transaction.TransactionAmountOut;
            transactionResponse.IsDCCTxn = transaction.IsDCCTransactionOut.ToString();
            transactionResponse.DCCAmount = transaction.DCCAmountOut;
            transactionResponse.DCCCurrency = transaction.DCCCurrencyOut;
            transactionResponse.DCCCurrencyExp = transaction.DCCCurrencyExponentOut.ToString();
            transactionResponse.FXExponentApplied = transaction.FXExponentAppliedOut.ToString();
            transactionResponse.FXRateApplied = transaction.FXRateAppliedOut;
            transactionResponse.IsLoyaltyTxn = transaction.IsLoyaltyTransactionOut.ToString();
            transactionResponse.DonationAmount = transaction.DonationAmountOut;
            transactionResponse.RedeemedAmount = transaction.RedeemedAmountOut;
            transactionResponse.HostMessage = transaction.HostMessageOut;
            transactionResponse.CVM = transaction.CVMOut;
            transactionResponse.GratuityAmount = transaction.GratuityAmountOut;
            transactionResponse.CashAmount = transaction.CashAmountOut;
            transactionResponse.TotalAmount = transaction.TotalTransactionAmountOut;
            transactionResponse.ICCAppFileName = transaction.ICCApplicationFileNameOut;
            transactionResponse.ICCAppPreferredName = transaction.ICCApplicationPreferredNameOut;
            transactionResponse.TransactionId = transaction.TransactionIDOut;
            transactionResponse.DiagRequestOut = transaction.DiagRequestOut;
            transactionResponse.TransactionStatus = transaction.TransactionStatusOut;
            transactionResponse.TransactionType = transaction.TransactionTypeOut;

            return transactionResponse;
        }
    }
}
