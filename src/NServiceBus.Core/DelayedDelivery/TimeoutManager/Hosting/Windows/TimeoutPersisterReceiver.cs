namespace NServiceBus.Timeout.Hosting.Windows
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.CircuitBreakers;
    using NServiceBus.ConsistencyGuarantees;
    using NServiceBus.DeliveryConstraints;
    using NServiceBus.Extensibility;
    using NServiceBus.Logging;
    using NServiceBus.Timeout.Core;
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Transport;

    class TimeoutPersisterReceiver : IDisposable
    {
        IQueryTimeouts timeoutsQuery;
        DefaultTimeoutManager timeoutManager;
        IDispatchMessages messageSender;

        public CriticalError CriticalError { get; set; }

        public TimeoutPersisterReceiver(IQueryTimeouts queryTimeouts, IDispatchMessages dispatchMessages, DefaultTimeoutManager timeoutManager, CriticalError criticalError)
        {
            timeoutsQuery = queryTimeouts;
            messageSender = dispatchMessages;
            this.timeoutManager = timeoutManager;
        }

        public int SecondsToSleepBetweenPolls { get; set; }
        public string DispatcherAddress { get; set; }
        public TimeSpan TimeToWaitBeforeTriggeringCriticalError { get; set; }

        public void Dispose()
        {
            //Injected
        }

        public void Start()
        {
            circuitBreaker = new RepeatedFailuresOverTimeCircuitBreaker("TimeoutStorageConnectivity", TimeToWaitBeforeTriggeringCriticalError,
                ex =>
                    CriticalError.Raise("Repeated failures when fetching timeouts from storage, endpoint will be terminated.", ex));

            timeoutManager.TimeoutPushed = TimeoutsManagerOnTimeoutPushed;

            SecondsToSleepBetweenPolls = 1;

            tokenSource = new CancellationTokenSource();

            StartPoller();
        }

        public void Stop()
        {
            timeoutManager.TimeoutPushed = null;
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

            var startSlice = DateTime.UtcNow.AddYears(-10);

            resetEvent.Reset();

            while (!cancellationToken.IsCancellationRequested)
            {
                if (nextRetrieval > DateTime.UtcNow)
                {
                    Thread.Sleep(SecondsToSleepBetweenPolls * 1000);
                    continue;
                }

                Logger.DebugFormat("Polling for timeouts at {0}.", DateTime.Now);

                DateTime nextExpiredTimeout;
                var timeoutDatas = timeoutsQuery.GetNextChunk(startSlice, out nextExpiredTimeout);

                foreach (var timeoutData in timeoutDatas)
                {
                    if (startSlice < timeoutData.Item2)
                    {
                        startSlice = timeoutData.Item2;
                    }
                  

                    var dispatchRequest = ControlMessageFactory.Create(MessageIntentEnum.Send);

                    dispatchRequest.Headers["Timeout.Id"] = timeoutData.Item1;

                    messageSender.Dispatch(dispatchRequest, new DispatchOptions(DispatcherAddress, new AtomicWithReceiveOperation(), new List<DeliveryConstraint>(), new ContextBag()));
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

        RepeatedFailuresOverTimeCircuitBreaker circuitBreaker;

        object lockObject = new object();
        ManualResetEvent resetEvent = new ManualResetEvent(true);
        DateTime nextRetrieval = DateTime.UtcNow;
        volatile bool timeoutPushed;
        CancellationTokenSource tokenSource;
    }
}