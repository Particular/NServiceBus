namespace NServiceBus.Timeout.Core
{
    using System;
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

            Persister.GetAll().ToList().ForEach(td =>
                TimeoutManager.PushTimeout(td));

            thread = new Thread(Poll) { IsBackground = true };
            thread.Start();
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
                    Logger.ErrorFormat("Failed to pop timeouts - " +  ex);
                }
            }
        }


        Thread thread;
        volatile bool stopRequested;
        static ILog Logger = LogManager.GetLogger("Timeouts");
    }
}


