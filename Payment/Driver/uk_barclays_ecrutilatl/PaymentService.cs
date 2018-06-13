using Acrelec.Library.Logger;
using Acrelec.Mockingbird.Payment.Configuration;
using Acrelec.Mockingbird.Payment.Contracts;
//using com.ingenico.cli.comconcert;
using System;
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
        /// 
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public Result Init(RuntimeConfiguration configuration)
        {
            Log.Info("Init method started...");

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

                using (var api = new ECRUtilATLApi())
                {
                    var connectResult = api.Connect();
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
        /// 
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
                    var connectResult = api.Connect();
                    Log.Info($"Connect Result: {connectResult}");

                    if (connectResult != ECRUtilATLErrMsg.OK)
                    {
                        return new Result<PaymentData>((ResultCode)connectResult);
                    }

                    var payResult = api.Pay(amount, out var payResponse);
                    Log.Info($"Pay Result: {payResult}");
                    Log.Info($"Pay Response Data: {payResponse.TransactionStatus}");

                    if (payResult != ECRUtilATLErrMsg.OK)
                    {
                        return new Result<PaymentData>((ResultCode)connectResult);
                    }
                    data.Result = (PaymentResult)Utils.GetTransactionOutResult(payResponse.TransactionStatus);
                    data.PaidAmount = amount;
                    XNode ticketDocument = null;
                    var ticketResult = api.Ticket(amount, out var ticketResponse);
                    Log.Info($"Ticket Result: {ticketResult}");
                    
                    //TEST
                    ticketResponse.NonECRUtilATLData = "transaction";

                    if (ticketResult == ECRUtilATLErrMsg.OK)
                    {
                        Log.Info($"transaction status: {ticketResponse.TransactionStatus}");
                        if (Utils.GetTransactionOutResult(ticketResponse.TransactionStatus) != TransactiontResult.Cancelled)
                        {
                            ticketDocument = XDocument.Parse(ticketResponse.NonECRUtilATLData);

                            var transactionInfo = RetrieveInfo(ticketDocument);
                            Log.Info($"transactionInfo Number: {transactionInfo.Number}");
                            Log.Info($"transactionInfo IsSignatureVerified: {transactionInfo.IsSignatureVerified}");

                            if (transactionInfo.IsSignatureVerified)
                            {
                                Log.Info("This transaction requires signature. We will reverse it");
                                var revResult = api.Reverse(transactionInfo.Number, out var revResponse);
                                return new Result<PaymentData>(ResultCode.TransactionCancelled, data: data);
                            }

                            CreateCustomerTicket(ticketDocument);
                            data.HasClientReceipt = true;
                        }
                    }
                    PersistTransaction(payResponse, ticketDocument);

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
        /// 
        /// </summary>
        /// <param name="ticketDocument"></param>
        /// <returns></returns>
        private TransactionInfo RetrieveInfo(XNode ticketDocument)
        {
            var result = new TransactionInfo();
            foreach (var receipt in ticketDocument.XPathSelectElements("CREDIT_CARD_RECEIPT/RECEIPT"))
            {
                foreach (var lf in receipt.Elements("LF"))
                {
                    if (lf.Attribute("ID_NAME").Value == "TRANSACTION NUMBER")
                    {   
                        result.Number = lf.Value.Replace("TXN ", "");
                    }

                    if(lf.Attribute("ID_NAME").Value == "CVM RESULT")
                    {
                        result.IsSignatureVerified = lf.Value == "SIGNATURE VERIFIED";
                    }
                }
            }

            Log.Info("Info retrieved with success");

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Shutdown()
        {
            Log.Info("Shutting down...");
            Program.ManualResetEvent.Set();
        }

        private void PersistTransaction(TransactionResponse result, XNode ticket)
        {
            try
            {
                var config = AppConfiguration.Instance;
                var outputDirectory = Path.GetFullPath(config.OutPath);
                var outputPath = Path.Combine(outputDirectory, $"{DateTime.Now:yyyyMMddHHmmss}_ticket.xml");

                if (!Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }

                var root = new XElement("transaction",
                    new XElement("response",
                        new XElement("date", DateTime.Now.ToString("yyyyMMdd")),
                        new XElement("time", DateTime.Now.ToString("HHmmss")),
                    //    new XElement("eposNumber", result.eposNumber),
                        new XElement("responseCode", result.AuthorizationCode),
                        new XElement("amount", result.TransactionAmount),
                        new XElement("paymentMode", result.EntryMethod),
                        new XElement("responseData", result.GemsReceiptID),
                        new XElement("currency", Utils.GetCurrencySymbol(Convert.ToInt32(result.Currency))),
                        new XElement("privateData", result.TransactionType),
                        new XElement("option", result.AID),
                        new XElement("nonConcertData", result.NonECRUtilATLData)
                    ));

                if (ticket != null)
                {
                    var customerReceipt = ticket.XPathSelectElement("//RECEIPT[@STYPE='CUSTOMER']");
                    if (customerReceipt != null)
                    {
                        root.Add(new XElement("customerReceipt",
                            customerReceipt.Elements().Select(_ =>
                            {
                                var tag = new XElement("tag", _.Value);
                                if (_.Attribute("ID") != null)
                                {
                                    tag.Add(new XAttribute("id", _.Attribute("ID").Value));
                                }

                                if (_.Attribute("ID_NAME") != null)
                                {
                                    tag.Add(new XAttribute("name", _.Attribute("ID_NAME").Value));
                                }
                                return tag;
                            })));
                    }


                    var merchantReceipt = ticket.XPathSelectElement("//RECEIPT[@STYPE='MERCHANT']");
                    if (merchantReceipt != null)
                    {
                        root.Add(new XElement("merchantReceipt",
                            merchantReceipt.Elements().Select(_ =>
                            {
                                var tag = new XElement("tag", _.Value);
                                if (_.Attribute("ID") != null)
                                {
                                    tag.Add(new XAttribute("id", _.Attribute("ID").Value));
                                }

                                if (_.Attribute("ID_NAME") != null)
                                {
                                    tag.Add(new XAttribute("name", _.Attribute("ID_NAME").Value));
                                }
                                return tag;
                            })));
                    }
                }

                var document = new XDocument(root);

                Log.Info($"Persist Transaction path: {ticketPath}");
                //Write the new ticket
                document.Save(outputPath);
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
        private static void CreateCustomerTicket(XNode ticket)
        {
            var customerReceipt = ticket.XPathSelectElement("//RECEIPT[@STYPE='CUSTOMER']");
            var ticketEntries = customerReceipt.Elements().Select(_ => _.Value).ToList();
            var ticketContent = string.Join("\n", ticketEntries);

            try
            {
                Log.Info("Persisting ticket to {0}", ticketPath);
                //Write the new ticket
                File.WriteAllText(ticketPath, ticketContent);
            }
            catch (Exception ex)
            {
                Log.Error("Error persisting ticket.");
                Log.Error(ex);
            }
        }
    }
}
