using Acrelec.Library.Logger;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Acrelec.Mockingbird.Payment.Settlement
{
    public class SettlementListener : IDisposable
    {
        public delegate void WorkerHandle(Action<string> fileSender);

        private TcpListener _tcpListener;

        private Thread _listenerThread;

        private WorkerHandle _worker;

        public SettlementListener(int port, WorkerHandle worker)
        {
            Log.Info($"Creating listener on port { port }...");

            _worker = worker;
            _tcpListener = new TcpListener(IPAddress.Any, port);
            _tcpListener.Start();

            _listenerThread = new Thread(DoWork);
            _listenerThread.Start();
        }

        public void Dispose()
        {
            _worker = null;

            _listenerThread.Abort();
            _listenerThread = null;

            _tcpListener.Stop();
            _tcpListener = null;

            Log.Info("Listener disposed.");
        }

        private void DoWork()
        {
            while (true)
            {
                try
                {
                    using (var socket = _tcpListener.AcceptSocket())
                    {
                        if (!socket.Connected)
                        {
                            Log.Info($"{ socket.RemoteEndPoint } NOT connected. Ignoring...");
                            continue;
                        }

                        Log.Info($"{ socket.RemoteEndPoint } connected.");

                        Log.Info("Executing worker...");
                        _worker(_ =>
                        {
                            Log.Info("Sending file started!");
                            socket.SendFile(_);
                            Log.Info("File sent!");
                        });
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Error sending file.");
                    Log.Error(ex);
                }
            }
        }
    }
}
