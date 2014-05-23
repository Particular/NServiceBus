namespace NServiceBus.Timeout.Hosting.Windows
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using CircuitBreakers;
    using Core;
    using Logging;
    using Transports;
    using Unicast;
    using Unicast.Transport;

    class TimeoutPersisterReceiver : IDisposable
    {
        public IPersistTimeouts TimeoutsPersister { get; set; }
        public ISendMessages MessageSender { get; set; }
        public int SecondsToSleepBetweenPolls { get; set; }
        public DefaultTimeoutManager TimeoutManager { get; set; }

        public void Dispose()
        {
            //Injected
        }

        public void Start()
        {
            TimeoutManager.TimeoutPushed = TimeoutsManagerOnTimeoutPushed;

            SecondsToSleepBetweenPolls = 1;

            tokenSource = new CancellationTokenSource();

            StartPoller();
        }

        public void Stop()
        {
            TimeoutManager.TimeoutPushed = null;
            tokenSource.Cancel();
            resetEvent.WaitOne();
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
                        circuitBreaker.Failure(ex);
                        return true;
                    });

                    StartPoller();
                }, TaskContinuationOptions.OnlyOnFaulted);
        }

        void Poll(object obj)
        {
            var cancellationToken = (CancellationToken) obj;

            var startSlice = DateTime.UtcNow.AddYears(-10);

            resetEvent.Reset();

            while (!cancellationToken.IsCancellationRequested)
            {
                if (nextRetrieval > DateTime.UtcNow)
                {
                    Thread.Sleep(SecondsToSleepBetweenPolls*1000);
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

                    MessageSender.Send(CreateTransportMessage(timeoutData.Item1), new SendOptions(Features.TimeoutManager.DispatcherAddress) { ReplyToAddress = Features.TimeoutManager.DispatcherAddress });
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

                // we cap the next retrieval to max 1 minute this will make sure that we trip the circuit breaker if we
                // loose connectivity to our storage. This will also make sure that timeouts added (during migration) direct to storage
                // will be picked up after at most 1 minute
                var maxNextRetrieval = DateTime.UtcNow + TimeSpan.FromMinutes(1);

                if (nextRetrieval > maxNextRetrieval)
                {
                    nextRetrieval = maxNextRetrieval;
                }

                Logger.DebugFormat("Polling next retrieval is at {0}.", nextRetrieval.ToLocalTime());
                circuitBreaker.Success();
            }

            resetEvent.Set();
        }

        static TransportMessage CreateTransportMessage(string timeoutId)
        {
            //use the dispatcher as the reply to address so that retries go back to the dispatcher q
            // instead of the main endpoint q
            var transportMessage = ControlMessage.Create();

            transportMessage.Headers["Timeout.Id"] = timeoutId;

            return transportMessage;
        }

        void TimeoutsManagerOnTimeoutPushed(TimeoutData timeoutData)
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

        static ILog Logger = LogManager.GetLogger<TimeoutPersisterReceiver>();

        RepeatedFailuresOverTimeCircuitBreaker circuitBreaker =
            new RepeatedFailuresOverTimeCircuitBreaker("TimeoutStorageConnectivity", TimeSpan.FromMinutes(2),
                ex =>
                    Configure.Instance.RaiseCriticalError(
                        "Repeated failures when fetching timeouts from storage, endpoint will be terminated.", ex));

        readonly object lockObject = new object();
        ManualResetEvent resetEvent = new ManualResetEvent(true);
        DateTime nextRetrieval = DateTime.UtcNow;
        volatile bool timeoutPushed;
        CancellationTokenSource tokenSource;
    }
}