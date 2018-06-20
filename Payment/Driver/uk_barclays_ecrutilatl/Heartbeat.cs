﻿using System;
using System.Threading;
using Acrelec.Library.Logger;
using Acrelec.Mockingbird.Payment.Configuration;
using Acrelec.Mockingbird.Payment.Contracts;
//using com.ingenico.cli.comconcert;

namespace Acrelec.Mockingbird.Payment
{
    public class Heartbeat : IDisposable
    {
        private Timer _timer;

        public Heartbeat()
        {
            _timer = new Timer(Beat, null, Timeout.Infinite, Timeout.Infinite);
            Instance = this;
        }

        public bool Alive { get; private set; }

        public static Heartbeat Instance { get; private set; }

        public void Dispose()
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
            _timer.Dispose();
            _timer = null;
        }

        public void Start()
        {
            var interval = AppConfiguration.Instance.HeartbeatInterval;
            if (interval < 30)
            {
                interval = 30;
            }
            var timeSpan = TimeSpan.FromSeconds(interval);
            _timer.Change(timeSpan, timeSpan);

            Beat(null);
        }

        private void Beat(object state)
        {
            var configuration = RuntimeConfiguration.Instance;
            if (configuration == null)
            {
                return;
            }

            try
            {
               
                using (var api = new ECRUtilATLApi())
                {
                    var connectResult = api.Connect();
                    if (connectResult != ECRUtilATLErrMsg.OK)
                    {
                        Alive = false;
                        Log.Debug($"Connect result: {connectResult}");
                        return;
                    }

                    var disconnectResult = api.Disconnect();
                    if (disconnectResult != ECRUtilATLErrMsg.OK)
                    {
                        Alive = false;
                        Log.Debug($"Disconnect result: {disconnectResult}");
                        return;
                    }

                    Alive = true;
                }
            }
            finally
            {
                Log.Debug($"Heartbeat status: {Alive}");
            }
        }
    }
}