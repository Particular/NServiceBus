namespace NServiceBus.Timeout.Hosting.Windows
{
    using System;
    using System.Threading;
    using Core;
    using Logging;
    using Transports;
    using Unicast.Queuing;
    using Unicast.Transport;

    public class TimeoutPersisterReceiver
    {
        static readonly ILog Logger = LogManager.GetLogger("NServiceBus.TimeoutPersisterReceiver");

        readonly object lockObject = new object();

        volatile bool stopRequested;
        volatile bool timeoutPushed;
        DateTime nextRetrieval = DateTime.UtcNow;
        Thread workerThread;

        public IPersistTimeouts TimeoutsPersister { get; set; }
        public ISendMessages MessageSender { get; set; }
        public int SecondsToSleepBetweenPolls { get; set; }
        public IManageTimeouts TimeoutManager { get; set; }

        private void TimeoutsManagerOnTimeoutPushed(object sender, TimeoutData timeoutData)
        {
            lock (lockObject)
            {
                if (nextRetrieval > timeoutData.Time)
                {
                    nextRetrieval = timeoutData.Time;
                }
                timeoutPushed = true;
            }
        }

        public void Start()
        {
            TimeoutManager.TimeoutPushed += TimeoutsManagerOnTimeoutPushed;

            SecondsToSleepBetweenPolls = 5;

            workerThread = new Thread(Poll) { IsBackground = true };

            workerThread.Start();
        }

        public void Stop()
        {
            stopRequested = true;

            workerThread.Join();
        }

        void Poll()
        {
            var pollingFailuresCount = 0;
            var startSlice = DateTime.UtcNow.AddYears(-10);

            while (!stopRequested)
            {
                if (nextRetrieval > DateTime.UtcNow)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(SecondsToSleepBetweenPolls));
                    continue;
                }

                try
                {
                    Logger.DebugFormat("Polling for timeouts at {0}.", DateTime.Now);

                    DateTime nextExpiredTimeout;
                    var timeoutDatas = TimeoutsPersister.GetNextChunk(startSlice, out nextExpiredTimeout);

                    foreach (var timeoutData in timeoutDatas)
                    {
                        if (startSlice < timeoutData.Item2)
                        {
                            startSlice = timeoutData.Item2;
                        }

                        MessageSender.Send(CreateTransportMessage(timeoutData.Item1), TimeoutDispatcherProcessor.TimeoutDispatcherAddress);
                    }

                    lock (lockObject)
                    {
                        //Check if nextRetrieval has been modified (This means that a push come in) and if it has check if it is earlier than nextExpiredTimeout time
                        if (!timeoutPushed)
                        {
                            nextRetrieval = nextExpiredTimeout;
                        }
                        else if (nextExpiredTimeout < nextRetrieval)
                        {
                            nextRetrieval = nextExpiredTimeout;
                        }

                        timeoutPushed = false;
                    }

                    Logger.DebugFormat("Polling next retrieval is at {0}.", nextRetrieval.ToLocalTime());

                    pollingFailuresCount = 0;
                }
                catch (Exception ex)
                {
                    if (pollingFailuresCount >= 10)
                    {
                        Logger.Fatal("Polling of timeouts has failed the maximum number of times.", ex);
                        throw; //This should bring down the whole endpoint
                    }

                    Logger.Error("Polling of timeouts failed.", ex);

                    pollingFailuresCount++;
                }
            }
        }

        static TransportMessage CreateTransportMessage(string timeoutId)
        {
            //use the dispatcher as the replytoaddress so that retries go back to the dispatcher q
            // instead of the main endpoint q
            var transportMessage = ControlMessage.Create(TimeoutDispatcherProcessor.TimeoutDispatcherAddress);

            transportMessage.Headers["Timeout.Id"] = timeoutId;

            return transportMessage;
        }
    }
}