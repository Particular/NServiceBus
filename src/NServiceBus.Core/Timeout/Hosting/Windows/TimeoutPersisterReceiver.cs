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
        public CriticalError CriticalError { get; set; }
        public Address DispatcherAddress { get; set; }
        public TimeSpan TimeToWaitBeforeTriggeringCriticalError { get; set; }
        internal DateTime NextRetrieval { get; private set; } = DateTime.UtcNow;

        public TimeoutPersisterReceiver()
        {            
        }

        public TimeoutPersisterReceiver(Func<DateTime> currentTimeProvider)
        {
            currentTime = currentTimeProvider;
        }

        public void Dispose()
        {
            //Injected
        }

        public void Start()
        {
            circuitBreaker = new RepeatedFailuresOverTimeCircuitBreaker("TimeoutStorageConnectivity", TimeToWaitBeforeTriggeringCriticalError,
                ex =>
                {
                    CriticalError.Raise("Repeated failures when fetching timeouts from storage, endpoint will be terminated.", ex);
                });

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
                        Logger.Warn("Failed to fetch timeouts from the timeout storage", ex);
                        circuitBreaker.Failure(ex);
                        return true;
                    });

                    StartPoller();
                }, TaskContinuationOptions.OnlyOnFaulted);
        }

        void Poll(object obj)
        {
            var cancellationToken = (CancellationToken)obj;

            var startSlice = currentTime().AddYears(-10);

            resetEvent.Reset();

            while (!cancellationToken.IsCancellationRequested)
            {
                startSlice = SpinOnce(startSlice);
                circuitBreaker.Success();
            }

            resetEvent.Set();
        }

        internal DateTime SpinOnce(DateTime startSlice)
        {
            if (NextRetrieval > currentTime())
            {
                Thread.Sleep(SecondsToSleepBetweenPolls * 1000);
                return startSlice;
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

                MessageSender.Send(CreateTransportMessage(timeoutData.Item1), new SendOptions(DispatcherAddress));
            }

            lock (lockObject)
            {
                // we cap the next retrieval to max 1 minute this will make sure that we trip the circuit breaker if we
                // loose connectivity to our storage. This will also make sure that timeouts added (during migration) direct to storage
                // will be picked up after at most 1 minute
                var maxNextRetrieval = currentTime() + TimeSpan.FromMinutes(1);

                NextRetrieval = nextExpiredTimeout > maxNextRetrieval ? maxNextRetrieval : nextExpiredTimeout;
            }
            Logger.DebugFormat("Polling next retrieval is at {0}.", NextRetrieval.ToLocalTime());
            return startSlice;
        }

        static TransportMessage CreateTransportMessage(string timeoutId)
        {
            //use the dispatcher as the reply to address so that retries go back to the dispatcher q
            // instead of the main endpoint q
            var transportMessage = ControlMessage.Create();

            transportMessage.Headers["Timeout.Id"] = timeoutId;

            return transportMessage;
        }

        internal void TimeoutsManagerOnTimeoutPushed(TimeoutData timeoutData)
        {
            lock (lockObject)
            {
                if (NextRetrieval > timeoutData.Time)
                {
                    NextRetrieval = timeoutData.Time;
                }
            }
        }

        static ILog Logger = LogManager.GetLogger<TimeoutPersisterReceiver>();

        RepeatedFailuresOverTimeCircuitBreaker circuitBreaker;

        readonly object lockObject = new object();
        ManualResetEvent resetEvent = new ManualResetEvent(true);        
        CancellationTokenSource tokenSource;
        private Func<DateTime> currentTime = () => DateTime.UtcNow;
    }
}