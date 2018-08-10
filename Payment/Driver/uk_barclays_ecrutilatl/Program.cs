using Acrelec.Library.Logger;
using Acrelec.Mockingbird.Payment.Configuration;
using Acrelec.Mockingbird.Payment.Contracts;
using Acrelec.Mockingbird.Payment.ExtensionMethods;
using Acrelec.Mockingbird.Payment.Settlement;
using System;
using System.ServiceModel;
using System.Threading;

namespace Acrelec.Mockingbird.Payment
{
    internal class Program
    {
        public static ManualResetEvent ManualResetEvent = new ManualResetEvent(false);

        public const string NAME = "UK_BARCLAYS_ECRUTILATL";

        private static void Main(string[] args)
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            Log.Info($"{assembly.GetTitle()} {assembly.GetFileVersion()} [build timestamp: {assembly.GetBuildTimestamp():yyyy/MM/dd HH:mm:ss}]");

            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

            var appConfig = AppConfiguration.Instance;

            using (var host = new ServiceHost(typeof(PaymentService), new Uri("net.pipe://localhost")))
            using (new Heartbeat())
            using (new SettlementListener(appConfig.SettlementTriggerPort, SettlementWorker.OnSettlement))
            {
                host.AddServiceEndpoint(typeof(IPaymentService), new NetNamedPipeBinding(), NAME);
                host.Open();

                Log.Info("Driver Service Running...");

                ManualResetEvent.WaitOne();
            }

            Log.Info("Driver application requested to shut down.");
        }

        //static void Main(string[] args)
        //{
        //    string ch = "Y";

        //    while (ch == "Y")
        //    {
        //        Console.Clear();

        //        Console.WriteLine("\n\tPayment Simulator");
        //        Console.WriteLine("\t=================\n");
        //        int amount = 500;

        //        using (var api = new ECRUtilATLApi())
        //        {
        //            var connectResult = api.Connect("1.1.1.2");
        //            if (connectResult != ECRUtilATLErrMsg.OK)
        //            {
        //                Console.ForegroundColor = ConsoleColor.Red;
        //                Console.WriteLine("\tConnect Result = {connectResult}");
        //                Console.WriteLine("\tExiting the Application...");
        //                Thread.Sleep(10);
        //                Environment.Exit(0);
        //            }
        //            else
        //            {
        //                Console.ForegroundColor = ConsoleColor.Green;
        //                Console.WriteLine($"\tConnection result = {connectResult}\n");
        //                Console.ForegroundColor = ConsoleColor.White;
        //            }

        //            Console.WriteLine("\nAmount set at £5");
        //            Console.WriteLine("----------------\n");
        //            var payResult = api.Pay(amount, out var payResponse);
        //            Console.WriteLine($"\tPay Result: {payResult}");

        //            Console.WriteLine("\tPay Transaction = " + Utils.GetTransactionOutResult(payResponse.TransactionStatus));

        //            // check if a swipe card used reverse the
        //            if (payResponse.EntryMethod == "2")
        //            {
        //                Console.WriteLine("\nThis transaction requires a signature. We will reverse it");
        //                var ReverseResult = api.Reverse(amount, out var ReverseResponse);

        //                //Console.WriteLine($"\nReverse Result: {ReverseResponse}");
        //                Console.WriteLine($"\nReverse Response Data: { Utils.GetTransactionOutResult(ReverseResponse.TransactionStatus)}\n");
        //            }

        //            //if ((ECRUtilATLErrMsg)Convert.ToInt32(payResponse.DiagRequestOut) == ECRUtilATLErrMsg.OK)
        //            //{
        //            // Console.WriteLine($"transaction status: {payResponse.TransactionStatus}");

        //            //Print receipt
        //            // Utils.PrintReceipt(payResponse);

        //            api.Disconnect();
        //            // Console.WriteLine("Do End of Day");
        //            // api.EndOfDayReport();
        //            //  }


        //        }

        //        Console.WriteLine("\nWould you like another transaction?(Y/N)");

        //        ch = Console.ReadLine().ToUpper();
        //    }

        //    Console.WriteLine("\nPress any key to exit");
        //    Console.ReadKey();

        //}

        private static void OnProcessExit(object sender, EventArgs e)
        {
            Log.Info("Shell driver application exiting");
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Log.Error((e.ExceptionObject as Exception).ToString());
        }
    }
}
