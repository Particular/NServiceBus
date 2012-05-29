namespace NServiceBus.Timeout.Core
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using Dispatch;
    using Unicast;
    using log4net;

    public class TimeoutRunner : IWantToRunWhenTheBusStarts
    {
        public IManageTimeouts TimeoutManager { get; set; }
        public IPersistTimeouts Persister { get; set; }
        public TimeoutDispatcher TimeoutDispatcher { get; set; }

        public void Run()
        {
            if (TimeoutManager == null)
                return;

            TimeoutManager.SagaTimedOut += (o, timeoutData) => TimeoutDispatcher.DispatchTimeout(timeoutData);

            CacheExistingTimeouts();

            thread = new Thread(Poll) { IsBackground = true };
            thread.Start();
        }

        void CacheExistingTimeouts()
        {
            var sw = new Stopwatch();
            sw.Start();

            Logger.DebugFormat("Going to fetch existing timeouts from persister ({0})", Persister.GetType().Name);

            var existingTimeouts = Persister.GetAll().ToList();

            Logger.DebugFormat("{0} timeouts loaded from storage in {1} seconds", existingTimeouts.Count, sw.Elapsed.TotalSeconds);

            existingTimeouts.ForEach(td => TimeoutManager.PushTimeout(td));

            sw.Stop();

            Logger.DebugFormat("Total time for cache priming {0} seconds", sw.Elapsed.TotalSeconds);
        }


        public void Stop()
        {
            stopRequested = true;
        }

        void Poll()
        {
            while (!stopRequested)
            {
                try
                {
                    TimeoutManager.PopTimeout();
                }
                catch (Exception ex)
                {
                    //intentionally swallow here to avoid this bringing the entire endpoint down.
                    //remove this when our sattelite support is introduced
                    Logger.ErrorFormat("Failed to pop timeouts - " + ex);
                }
            }
        }


        Thread thread;
        volatile bool stopRequested;
        static ILog Logger = LogManager.GetLogger("Timeouts");
    }
}


