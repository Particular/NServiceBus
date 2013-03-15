namespace NServiceBus.Timeout.Hosting.Windows
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Core;
    using Logging;
    using Transports;
    using Unicast.Transport;
    using Utils;

    public class TimeoutPersisterReceiver
    {

        public IPersistTimeouts TimeoutsPersister { get; set; }
        public ISendMessages MessageSender { get; set; }
        public int SecondsToSleepBetweenPolls { get; set; }
        public IManageTimeouts TimeoutManager { get; set; }

        public void Start()
        {
            TimeoutManager.TimeoutPushed += TimeoutsManagerOnTimeoutPushed;

            SecondsToSleepBetweenPolls = 5;

            tokenSource = new CancellationTokenSource();

            StartPoller();
        }

        public void Stop()
        {
            tokenSource.Cancel();
        }

        void StartPoller()
        {
            var token = tokenSource.Token;

            Task.Factory
               .StartNew(Poll, token, TaskCreationOptions.LongRunning)
               .ContinueWith(t =>
               {
                   t.Exception.Handle(ex =>
                   {
                       Logger.Warn("Failed to fetch timeouts from the timeout storage");
                       circuitBreaker.Execute(() => Configure.Instance.RaiseCriticalError("Repeted failures when fetching timeouts from storage, endpoint will be terminated", ex));
                       return true;
                   });

                   Thread.Sleep(1000);

                   StartPoller();
               }, TaskContinuationOptions.OnlyOnFaulted);
        }


        void Poll(object obj)
        {
            var cancellationToken = (CancellationToken)obj;

            var startSlice = DateTime.UtcNow.AddYears(-10);

            while (!cancellationToken.IsCancellationRequested)
            {
                if (nextRetrieval > DateTime.UtcNow)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(SecondsToSleepBetweenPolls));
                    continue;
                }

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

        void TimeoutsManagerOnTimeoutPushed(object sender, TimeoutData timeoutData)
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




        readonly object lockObject = new object();

        CancellationTokenSource tokenSource;
        volatile bool timeoutPushed;
        DateTime nextRetrieval = DateTime.UtcNow;
        readonly CircuitBreaker circuitBreaker = new CircuitBreaker(10, TimeSpan.FromSeconds(30));

        static readonly ILog Logger = LogManager.GetLogger(typeof(TimeoutPersisterReceiver));

    }
}