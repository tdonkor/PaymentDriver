using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Acrelec.Library.Logger;
using ECRUtilATLLib;


namespace Acrelec.Mockingbird.Payment
{
    public class ECRUtilATLApi: IDisposable
    {
        TerminalIPAddress termimalIPAddress;
        StatusClass termimalStatus;
        InitTxnReceiptPrint initTxnReceiptPrint;
        GetTerminalStatus getTerminalStatus;

        public ECRUtilATLApi()
        {
           
        }

        public void Dispose()
        {
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool FreeLibrary(IntPtr hModule);

        /// <summary>
        /// Check the pre=requistes have been set:
        /// Ip Address is 1.1.1.2
        /// Status is at IDLE
        /// Disable the printing to print from the Transaction response
        /// </summary>
        /// <returns></returns>
        public string Connect()
        {
            //set static IP address
            termimalIPAddress = new TerminalIPAddress();
            termimalIPAddress.IPAddressIn = "1.1.1.2";
            termimalIPAddress.SetIPAddress();

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
                Log.Info("Printing disabled");
            else
                Log.Info("Printing enabled");



            //check the result 
            return Utils.DisplayDiagReqOutput(Convert.ToInt32(termimalIPAddress.DiagRequestOut));

        }

        /// <summary>
        /// Disconect from the Pin Pad
        /// </summary>
        public void Disconnect()
        {
            Log.Info("Disconnecting...");
            //return COMConcertLibrary.ConcertStop_();
        }

        public void Pay(int amount/* out result*/)
        {
            Log.Info($"Executing payment - Amount: {amount}");

            var paymentType = TransactionType.Sale; 
        }

       
        //Refund();
        //Reverse()
        //Ticket();
        //EndOfDayReport()

    }
}
