using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
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
        GetTerminalStatus getTerminalStatus;
        TransactionResponse transactionResponse;
        TimeDateClass setTimeDate;
        GetAcquirerListClass getAcquirerList;
        SettlementClass getSettlement;
        SettlementRequest settlementRequest;
        SignatureClass checkSignature;
        VoiceReferralClass voiceReferral;
        CancelTransactionClass cancelTransaction;
       



        /// <summary>
        /// Constructor initialise All the objects needed
        /// </summary>
        public ECRUtilATLApi()
        {
            transaction = new TransactionClass();
            termimalStatus = new StatusClass();
            getTerminalStatus = new GetTerminalStatus();
            setTimeDate = new TimeDateClass();
            getAcquirerList = new GetAcquirerListClass();
            getSettlement = new SettlementClass();
            checkSignature = new SignatureClass();
            voiceReferral = new VoiceReferralClass();
            cancelTransaction = new CancelTransactionClass();
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

            Log.Info($"IP Address in connect() : {ipAddress}");

            // check the status is at IDLE
            getTerminalStatus = new GetTerminalStatus();

            //log the status
            Log.Info($"status = {getTerminalStatus.GetTheTerminalStatus()}");


            // disable the receipt Printing
            initTxnReceiptPrint = new InitTxnReceiptPrint();
            initTxnReceiptPrint.StatusIn = (short)TxnReceiptState.TXN_RECEIPT_DISABLED;
            initTxnReceiptPrint.SetTxnReceiptPrintStatus();


            //check printing disabled
            if ((int.Parse(initTxnReceiptPrint.DiagRequestOut) == 0))
                Log.Info("apiInitTxnReceiptPrint OFF");
            else
                Log.Info("apiInitTxnReceiptPrint ON");

            //check time is set
            SetTime();

            //check theconnection  result 
            return (ECRUtilATLErrMsg)Convert.ToInt32(termimalIPAddress.DiagRequestOut);
        }

        /// <summary>
        /// Disconect 
        /// </summary>
        public ECRUtilATLErrMsg Disconnect()
        {

            Log.Info("Disconnecting...Reset the transaction");
            transaction.Reset();
            // log the status
            Log.Info($"status = {getTerminalStatus.GetTheTerminalStatus()}");

            ECRUtilATLErrMsg disconnResult = ECRUtilATLErrMsg.UknownValue;

            if ((ECRUtilATLErrMsg)Convert.ToInt32(transactionResponse.DiagRequestOut) == ECRUtilATLErrMsg.OK)
                disconnResult = ECRUtilATLErrMsg.OK;
            else
                disconnResult = ECRUtilATLErrMsg.UknownValue;

            return disconnResult;

        }

        /// <summary>
        /// The Payment
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public ECRUtilATLErrMsg Pay(int amount, out TransactionResponse result)
        {
            int intAmount;
            var paymentType = TransactionType.Sale;

            Log.Info($"Executing payment - Amount: {amount}");

            //check amount is valid
            intAmount = Utils.GetNumericAmountValue(amount);

            if (intAmount == 0)
                throw new Exception("Error in input");

            // Transaction Sale details
            //
            ECRUtilATLTransaction(amount, Utils.GetTransactionTypeString(Convert.ToInt32(paymentType)));

            //check if transaction is a signature
           // checkSignature.CheckSignReq();
            //if (transaction.EntryMethodOut == "2")
            //{
            //    //reverse or cancel the transaction

            //    Log.Info($"status before cancel = {getTerminalStatus.GetTheTerminalStatus()}");
            //    Log.Info($"Cancelling transaction");
            //    cancelTransaction.Launch();

            //    Log.Info($"Cancel  is {((ECRUtilATLErrMsg)Convert.ToInt32(cancelTransaction.DiagRequestOut)).ToString()}");
            //}

            result = PopulateResponse(transaction);
            return (ECRUtilATLErrMsg)Convert.ToInt32(transaction.DiagRequestOut);

        }

        //public ECRUtilATLErrMsg Reverse(int amount, out TransactionResponse result)
        //{
        //    int intAmount;
        //    var paymentType = TransactionType.Reversal;

        //    Log.Info($"Executing Reversal - Amount: {amount}");

        //    //check amount is valid
        //    intAmount = Utils.GetNumericAmountValue(amount);

        //    if (intAmount == 0)
        //        throw new Exception("Error in input");

        //    // Transaction Sale details
        //    //
        //    ECRUtilATLTransaction(amount, Utils.GetTransactionTypeString(Convert.ToInt32(paymentType)));
        //    result = PopulateResponse(transaction);

        //    return (ECRUtilATLErrMsg)Convert.ToInt32(transaction.DiagRequestOut);

        //}

        /// <summary>
        /// End of day report
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public SettlementClass EndOfDayReport()
        {

            Log.Info("Printing end of day report...");

            //Get Acquirer List
            getAcquirerList.Launch();

            //do the settlement

            getSettlement.AcquirerIndexIn = settlementRequest.AcquirerIndex;
            getSettlement.SettlementParamIn = (short)settlementRequest.SettlementParameter;
            getSettlement.DoSettlement();

            if ((ECRUtilATLErrMsg)(Convert.ToInt32(getSettlement.DiagRequestOut)) == ECRUtilATLErrMsg.OK)
                return getSettlement;
            else return null;

        }

        /// <summary>
        /// Set the Ped Time
        /// </summary>
        /// <returns></returns>
        public string SetTime()
        {
            setTimeDate.DayIn = DateTime.Now.Day.ToString();
            setTimeDate.MonthIn = DateTime.Now.Month.ToString();
            setTimeDate.YearIn = DateTime.Now.Year.ToString();
            setTimeDate.DayIn = DateTime.Now.Day.ToString();
            setTimeDate.HourIn = DateTime.Now.Hour.ToString();
            setTimeDate.MinuteIn = DateTime.Now.Minute.ToString();
            setTimeDate.SecondIn = DateTime.Now.Second.ToString();

            setTimeDate.SetTimeDate();

            Log.Info($"\tDateTime: {setTimeDate.DayIn}:{setTimeDate.MonthIn}:{ setTimeDate.YearIn}:{ setTimeDate.HourIn}:{ setTimeDate.MinuteIn}:{setTimeDate.SecondIn}");
            return Utils.DisplayDiagReqOutput(Convert.ToInt32(setTimeDate.DiagRequestOut));
        }

        /// <summary>
        ///  Do the transaction
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="transactionType"></param>
        private void ECRUtilATLTransaction(int amount, string transactionType)
        {
            Random randomNum = new Random();

            transaction.MessageNumberIn = randomNum.Next(100).ToString();
            transaction.TransactionTypeIn = transactionType;
            transaction.Amount1In = amount.ToString();
            transaction.Amount1LabelIn = "Amount 1";
            transaction.Amount2In = "0";
            transaction.Amount2LabelIn = string.Empty;
            transaction.Amount3In = "0";
            transaction.Amount3LabelIn = string.Empty;
            transaction.Amount4In = "0";
            transaction.Amount4LabelIn = string.Empty;
            transaction.ReferenceIn = string.Empty;
            transaction.TransactionIDIn = string.Empty;
            transaction.AuthorizationCodeIn = string.Empty;
            transaction.OfferPWCBIn = (short)OfferPWCBState.PWCB_DISABLED;


            

            //Set voice referral
            //voiceReferral.AuthorisationStatusIn = 1;
            //voiceReferral.AuthorisationCodeIn = string.Empty;
            //voiceReferral.SetAuthorisation();
            //Log.Info($"VoiceReferral is {((ECRUtilATLErrMsg)Convert.ToInt32(voiceReferral.DiagRequestOut)).ToString()}");

            Log.Info("Transaction launched...");
            transaction.Launch();

        }

        /// <summary>
        /// Populate the transaction response Object
        /// </summary>
        /// <param name="transaction"></param>
        /// <returns>transactionResponse</returns>
        private TransactionResponse PopulateResponse(TransactionClass transaction)
        {

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
