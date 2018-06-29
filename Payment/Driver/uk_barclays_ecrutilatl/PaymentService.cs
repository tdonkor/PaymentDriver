using Acrelec.Library.Logger;
using Acrelec.Mockingbird.Payment.Configuration;
using Acrelec.Mockingbird.Payment.Contracts;
using System;
using System.Text;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Acrelec.Mockingbird.Payment
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall)]
    public class PaymentService : IPaymentService
    {
        private static readonly string ticketPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "ticket");
       

        /// <summary>
        /// Get the configuratiion data
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public Result Init(RuntimeConfiguration configuration)
        {
            Log.Info("Init method started...");

            Log.Info($"IP Address value : {configuration.IpAddress}");

            try
            {
                if (configuration == null)
                {
                    Log.Info("Can not set configuration to null.");
                    return ResultCode.GenericError;
                }

                if (configuration.PosNumber <= 0)
                {
                    Log.Info($"Invalid PosNumber {configuration.PosNumber}.");
                    return ResultCode.GenericError;
                }
                if (configuration.IpAddress == string.Empty)
                {
                    Log.Info($"Invalid IPAddress {configuration.IpAddress}.");
                    return ResultCode.GenericError;
                }

                using (var api = new ECRUtilATLApi())
                {
                    var connectResult = api.Connect(configuration.IpAddress);
                    
                    Log.Info($"Connect Result: {connectResult}");

                    if (connectResult != ECRUtilATLErrMsg.OK)
                    {
                        return new Result<PaymentData>((ResultCode)connectResult);
                    }

                    RuntimeConfiguration.Instance = configuration;
                    Heartbeat.Instance.Start();
                    Log.Info("Init success!");

                    return ResultCode.Success;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return ResultCode.GenericError;
            }
            finally
            {
                Log.Info("Init method finished.");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Result Test()
        {
            var alive = Heartbeat.Instance?.Alive == true;
            Log.Debug($"Test status: {alive}");
            return alive ? ResultCode.Success : ResultCode.GenericError;
        }

        /// <summary>
        /// Payment method
        /// </summary>
        /// <param name="amount"></param>
        /// <returns></returns>
        public Result<PaymentData> Pay(int amount)
        {
            Log.Info("Pay method started...");
            Log.Info($"Amount = {amount}.");

            try
            {
                if (File.Exists(ticketPath))
                {
                    File.Delete(ticketPath);
                }

                if (amount <= 0)
                {
                    Log.Info("Invalid pay amount.");
                    return ResultCode.GenericError;
                }

                var config = RuntimeConfiguration.Instance;

                var data = new PaymentData();

                Log.Info("Calling payment driver...");

                using (var api = new ECRUtilATLApi())
                {
                    var connectResult = api.Connect(config.IpAddress);
                    Log.Info($"Connect Result: {connectResult}");

                    if (connectResult != ECRUtilATLErrMsg.OK)
                    {
                        return new Result<PaymentData>((ResultCode)connectResult);
                    }

                    var payResult = api.Pay(amount, out var payResponse);
                    Log.Info($"Pay Result: {payResult}");
                    Log.Info($"Pay Response Data: { Utils.GetTransactionOutResult(payResponse.TransactionStatus)}");

                    if (payResult != ECRUtilATLErrMsg.OK)
                    {
                        return new Result<PaymentData>((ResultCode)connectResult);
                    }
                    data.Result = (PaymentResult)Utils.GetTransactionOutResult(payResponse.TransactionStatus);
                    data.PaidAmount = amount;

                   //check if a swipe card used reverse the 
                   //if (payResponse.EntryMethod == "2")
                   // {
                   //     Log.Info("This transaction requires signature. We will reverse it");
                   //     var ReverseResult = api.Reverse(amount, out var ReverseResponse);

                   //     Log.Info($"Reverse Result: {ReverseResponse}");
                   //     Log.Info($"Revers Response Data: { Utils.GetTransactionOutResult(ReverseResponse.TransactionStatus)}");
                   // }


                    //create customer receipt
                    if ((ECRUtilATLErrMsg) Convert.ToInt32(payResponse.DiagRequestOut) == ECRUtilATLErrMsg.OK)    
                        {
                        Log.Info($"transaction status: {payResponse.TransactionStatus}");
                        if (Utils.GetTransactionOutResult(payResponse.TransactionStatus) == TransactiontResult.Successful)
                        {
                            CreateCustomerTicket(payResponse);
                            data.HasClientReceipt = true;
                        }
                    }

                   //persist the transaction
                   PersistTransaction(payResponse);

                    var disconnectResult = api.Disconnect();
                    Log.Info($"Disconnect Result: {disconnectResult}");
                }

                Log.Info("Payment succeeded.");

                return new Result<PaymentData>(ResultCode.Success, data: data);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return ResultCode.GenericError;
            }
            finally
            {
                Log.Info("Pay method finished.");
            }
        }

        

        /// <summary>
        /// Shutdown
        /// </summary>
        public void Shutdown()
        {
            Log.Info("Shutting down...");
            Program.ManualResetEvent.Set();
        }

        /// <summary>
        /// Persist the transaction as Text file
        /// with Customer and Merchant receiept
        /// </summary>
        /// <param name="result"></param>
        private void PersistTransaction(TransactionResponse result)
        {
            try
            {
                var config = AppConfiguration.Instance;
                var outputDirectory = Path.GetFullPath(config.OutPath);
                var outputPath = Path.Combine(outputDirectory, $"{DateTime.Now:yyyyMMddHHmmss}_ticket.txt");

                if (!Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }

                StringBuilder customerReceipt = new StringBuilder();
                StringBuilder merchantReceipt = new StringBuilder();

                //get the reponse details for the ticket
                customerReceipt.Append($"\nCUSTOMER RECEIPT\n");
                customerReceipt.Append($"================\n\n");
                customerReceipt.Append($"MERCHANT NAME:  {result.MerchantName}\n");         
                customerReceipt.Append($"MERCHANT ADDR1: {result.MerchantAddress1}\n");   
                customerReceipt.Append($"MERCHANT ADDR2: {result.MerchantAddress2}\n");    
                customerReceipt.Append($"ACQUIRER MERCHANT ID: {result.AcquirerMerchantID}\n");
                customerReceipt.Append($"ENTRY METHOD: {Utils.CardEntryMethod(result.EntryMethod)}\n");
                customerReceipt.Append($"TID: {result.TerminalId}\n");      
                customerReceipt.Append($"AID: {result.AID}\n");                 
                customerReceipt.Append($"CARD SCHEME NAME: {result.CardSchemeName}\n");     
                customerReceipt.Append($"PAN: {result.PAN}\n");              
                customerReceipt.Append($"PAN SEQUNCE NUMBER : PAN.SEQ {result.PANSeqNum}\n");      
                customerReceipt.Append($"TRANSACTION TYPE: {Utils.GetTransactionTypeString(Convert.ToInt32(result.TransactionType))}\n");
                customerReceipt.Append($"Currency:{Utils.GetCurrencySymbol(Convert.ToInt32(result.Currency))}\n");
                customerReceipt.Append($"AMOUNT: {Utils.GetCurrencySymbol(Convert.ToInt32(result.Currency))}{Utils.FormatReceiptAmount(result.TransactionAmount)}\n");  
                customerReceipt.Append($"TOTAL AMOUNT: {Utils.FormatReceiptAmount(result.TotalAmount)}\n");   
                customerReceipt.Append($"CVM: {Utils.CardVerification(result.CVM)}\n");
                customerReceipt.Append($"HOST MESSAGE: {result.HostMessage}\n");
                customerReceipt.Append("***********************\n");
                customerReceipt.Append($"{Utils.DiagTxnStatus(result.TransactionStatus)}\n");
                customerReceipt.Append("***********************\n");


                //get the reponse details for the ticket
                merchantReceipt.Append($"\n\nMERCHANT RECEIPT\n");
                merchantReceipt.Append($"================\n\n");
                merchantReceipt.Append($"Acquirer Merchant ID: {result.AcquirerMerchantID}\n");
                merchantReceipt.Append($"MERCHANT NAME:  {result.MerchantName}\n");
                merchantReceipt.Append($"MERCHANT ADDR1: {result.MerchantAddress1}\n");
                merchantReceipt.Append($"MERCHANT ADDR2: {result.MerchantAddress2}\n");
                merchantReceipt.Append($"ENTRY METHOD: {Utils.CardEntryMethod(result.EntryMethod)}\n");
                merchantReceipt.Append($"TID: {result.TerminalId}\n");
                merchantReceipt.Append($"AID: {result.AID}\n");
                merchantReceipt.Append($"CARD SCHEME NAME: {result.CardSchemeName}\n");
                merchantReceipt.Append($"PAN: {result.PAN}\n");
                merchantReceipt.Append($"PAN SEQUNCE NUMBER : PAN.SEQ {result.PANSeqNum}\n");
                merchantReceipt.Append($"TRANSACTION TYPE: {Utils.GetTransactionTypeString(Convert.ToInt32(result.TransactionType))}\n");
                merchantReceipt.Append($"Currency: {Utils.GetCurrencySymbol(Convert.ToInt32(result.Currency))}\n");
                merchantReceipt.Append($"AMOUNT: {Utils.GetCurrencySymbol(Convert.ToInt32(result.Currency))}{Utils.FormatReceiptAmount(result.TransactionAmount)}\n");
                merchantReceipt.Append($"TOTAL AMOUNT: {Utils.FormatReceiptAmount(result.TotalAmount)}\n");
                merchantReceipt.Append($"Transaction DATE TIME: {result.TxnDateTime}\n");
                merchantReceipt.Append($"CVM:{Utils.CardVerification(result.CVM)}\n");
                merchantReceipt.Append($"HOST MESSAGE{result.HostMessage}\n");
                merchantReceipt.Append($"ACQUIRER RESPONSE CODE: {result.AcquirerResponseCode}\n");
                merchantReceipt.Append($"RECEIPT NUMBER: {result.ReceiptNumber}\n");
                merchantReceipt.Append($"\nTRANSACTION STATUS OUT: \n");

                merchantReceipt.Append("***********************\n");
                merchantReceipt.Append($"{Utils.DiagTxnStatus(result.TransactionStatus)}\n");
                merchantReceipt.Append("***********************\n");

                Log.Info("Persisting ticket to {0}", outputPath);
                ////Write the new ticket
                File.WriteAllText(outputPath, customerReceipt.ToString() + merchantReceipt.ToString());
            }
            catch (Exception ex)
            {
                Log.Error("Persist Transaction exception.");
                Log.Error(ex);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ticket"></param>
        private static void CreateCustomerTicket(TransactionResponse ticket)
        {
            StringBuilder ticketContent = new StringBuilder();

            //get the reponse details for the ticket
            ticketContent.Append($"CUSTOMER RECEIPT\n");
            ticketContent.Append($"================\n\n");

            ticketContent.Append($"{ticket.MerchantName}\n");         // Merchant name
            ticketContent.Append($"{ticket.MerchantAddress1}\n");     // Merchant Addr1
            ticketContent.Append($"{ticket.MerchantAddress2}\n");     // Merchant Addr2
            ticketContent.Append($"{ticket.AcquirerMerchantID}\n");   // Aquirer Merchant ID name
            ticketContent.Append($"{ticket.TerminalId}\n");           // Terminal ID
            ticketContent.Append($"{ticket.AID}\n");                  // AID
            ticketContent.Append($"{ticket.CardSchemeName}\n");       // Card Schema name
            ticketContent.Append($"{ticket.PAN}\n");                  // Pan
            ticketContent.Append($"CVM:{Utils.CardVerification(ticket.CVM)}\n");
            ticketContent.Append($"ICC PAN.SEQ {ticket.PANSeqNum}\n");            // Pan Seq num
            ticketContent.Append($"\n{Utils.GetTransactionTypeString(Convert.ToInt32(ticket.TransactionType))}\n");      // Transaction Type
            ticketContent.Append("Card Holder Copy\nPlease keep this Receipt\nfor your records\n");

            ticketContent.Append($"\nAmount: {Utils.GetCurrencySymbol(Convert.ToInt32(ticket.Currency))}{Utils.FormatReceiptAmount(ticket.TransactionAmount)}\n");    //Amount
            ticketContent.Append("\t----------\n");
            ticketContent.Append($"Total {Utils.FormatReceiptAmount(ticket.TotalAmount)}\n");    //Total Amount
            ticketContent.Append("\t----------\n\n");
            ticketContent.Append($"{DateTime.Now.ToShortTimeString()} {DateTime.Now.ToShortDateString()}");
            ticketContent.Append("\nVerified by Pin\nTHANK YOU\n");
            ticketContent.Append($"{ticket.HostMessage}\n");          // Host Message

            ticketContent.Append("\n***********************\n");
            ticketContent.Append($"{Utils.DiagTxnStatus(ticket.TransactionStatus)}\n");
            ticketContent.Append("***********************\n");

            try
            {
                Log.Info("Persisting Customer ticket to {0}", ticketPath);
                ////Write the new ticket
                File.WriteAllText(ticketPath, ticketContent.ToString());
            }
            catch (Exception ex)
            {
                Log.Error("Error persisting ticket.");
                Log.Error(ex);
            }
        }
    }
}
