using System;
using System.Runtime.InteropServices;
using Acrelec.Library.Logger;
//
//namespace Acrelec.Mockingbird.Payment
////    public class ComConcertApi : IDisposable
//    {
//        public ComConcertApi()
//        {
//            COMConcertLibrary.ConcertLog_SetPath_("Logs");
//            COMConcertLibrary.ConcertLog_(1);
//        }

//        public void Dispose()
//        {
//        }

//        [DllImport("kernel32.dll", SetLastError = true)]
//        public static extern bool FreeLibrary(IntPtr hModule);

//        public COMConcertLibrary.ConcertErrMsg Connect(string port)
//        {
//            if (!uint.TryParse(port.Replace("COM", ""), out var portNumber))
//            {
//                return COMConcertLibrary.ConcertErrMsg.WrongParameter;
//            }

//            Log.Info($"Connecting to port {port}");

//            return COMConcertLibrary.ConcertStart_(portNumber,
//                COMConcertLibrary.SERIAL_BAUD_9600,
//                COMConcertLibrary.SERIAL_DATABITS_7,
//                COMConcertLibrary.SERIAL_PARITY_EVEN,
//                COMConcertLibrary.SERIAL_STOPBITS_1,
//                COMConcertLibrary.SERIAL_FLOWCONTROL_HARDWARE);
//        }

//        public COMConcertLibrary.ConcertErrMsg Disconnect()
//        {
//            Log.Info("Disconnecting...");
//            return COMConcertLibrary.ConcertStop_();
//        }

//        public COMConcertLibrary.ConcertErrMsg Pay(int amount, uint timeout, bool forceOnline, out COMConcertLibrary.SerialPortReceive result)
//        {
//            Log.Info($"Executing payment - Amount: {amount}");

//            var paymentMode = COMConcertLibrary.PAYMENT_MODE_EFT_HANDLING;
//            var paymentType = COMConcertLibrary.PAYMENT_TYPE_SALE;

//            var dataToSend = new COMConcertLibrary.SerialPortSend(1, amount, true,
//                paymentMode, paymentType, 826 /*GBP*/, null, false, true);

//            result = new COMConcertLibrary.SerialPortReceive();

//            return COMConcertLibrary.ConcertTransaction_(ref dataToSend, ref result, timeout);
//        }

//        public COMConcertLibrary.ConcertErrMsg Refund(int amount, uint timeout, bool forceOnline, out COMConcertLibrary.SerialPortReceive result)
//        {
//            Log.Info($"Executing refund - Amount: {amount}");

//            var paymentMode = COMConcertLibrary.PAYMENT_MODE_EFT_HANDLING;
//            var paymentType = COMConcertLibrary.PAYMENT_TYPE_REFUND;

//            var dataToSend = new COMConcertLibrary.SerialPortSend(1, amount, true,
//                paymentMode, paymentType, 826 /*GBP*/, null, false, true);

//            result = new COMConcertLibrary.SerialPortReceive();

//            return COMConcertLibrary.ConcertTransaction_(ref dataToSend, ref result, timeout);
//        }

//        public COMConcertLibrary.ConcertErrMsg Reverse(string transactionNumber, uint timeout, out COMConcertLibrary.SerialPortReceive result)
//        {   
//            Log.Info($"Executing reverse");

//            var paymentMode = COMConcertLibrary.PAYMENT_MODE_EFT_HANDLING;

//            var dataToSend = new COMConcertLibrary.SerialPortSend(1, 0, true,
//                paymentMode, 0, 826 /*GBP*/, $"REV{transactionNumber}", false, true);

//            result = new COMConcertLibrary.SerialPortReceive();

//            return COMConcertLibrary.ConcertTransaction_(ref dataToSend, ref result, timeout);
//        }

//        public COMConcertLibrary.ConcertErrMsg Ticket(uint timeout, out COMConcertLibrary.SerialPortReceive result)
//        {
//            Log.Info("Printing ticket...");
//            var paymentMode = COMConcertLibrary.PAYMENT_MODE_EFT_HANDLING;
//            var paymentType = COMConcertLibrary.PAYMENT_TYPE_SALE;

//            var dataToSend = new COMConcertLibrary.SerialPortSend(1, 0, true,
//                paymentMode, paymentType, 826 /*GBP*/, "TICKET", false, true);

//            result = new COMConcertLibrary.SerialPortReceive();

//            return COMConcertLibrary.ConcertTransaction_(ref dataToSend, ref result, timeout);
//        }

//        public COMConcertLibrary.ConcertErrMsg EndOfDayReport(uint timeout, out COMConcertLibrary.SerialPortReceive result)
//        {
//            Log.Info("Printing end of day report...");

//            var paymentMode = COMConcertLibrary.PAYMENT_MODE_EFT_HANDLING;
//            var paymentType = COMConcertLibrary.PAYMENT_TYPE_SALE;

//            var dataToSend = new COMConcertLibrary.SerialPortSend(1, 0, true,
//                paymentMode, paymentType, 826 /*GBP*/, "EOD", false, true);

//            result = new COMConcertLibrary.SerialPortReceive();

//            var transactionResult = COMConcertLibrary.ConcertTransaction_(ref dataToSend, ref result, timeout);

//            if (transactionResult != COMConcertLibrary.ConcertErrMsg.None)
//            {
//                return transactionResult;
//            }

//            dataToSend = new COMConcertLibrary.SerialPortSend(1, 0, true,
//                paymentMode, paymentType, 826 /*GBP*/, "REPORT", false, true);

//            return COMConcertLibrary.ConcertTransaction_(ref dataToSend, ref result, timeout);
//        }
//    }
//}
