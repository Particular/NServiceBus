namespace NServiceBus.DelayedDelivery.TimeoutManager
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

    class ExpiredTimeoutsPoller : IDisposable
    {
        public ExpiredTimeoutsPoller(IQueryTimeouts timeoutsFetcher, IDispatchMessages dispatcher, string dispatcherAddress, RepeatedFailuresOverTimeCircuitBreaker circuitBreaker)
        {
            this.timeoutsFetcher = timeoutsFetcher;
            this.dispatcher = dispatcher;
            this.dispatcherAddress = dispatcherAddress;
            this.circuitBreaker = circuitBreaker;
        }

        public void Start()
        {
            tokenSource = new CancellationTokenSource();

            StartPoller();
        }

        public void Stop()
        {
            tokenSource.Cancel();

            resetEvent.WaitOne();
        }

        public void NewTimeoutRegistered(DateTime expiryTime)
        {
            lock (lockObject)
            {
                if (nextRetrieval > expiryTime)
                {
                    nextRetrieval = expiryTime;
                }
                timeoutPushed = true;
            }
        }

        public void Dispose()
        {
            //Injected
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
                    Thread.Sleep(1000);
                    continue;
                }

                Logger.DebugFormat("Polling for timeouts at {0}.", DateTime.Now);

                DateTime nextExpiredTimeout;
                var timeoutDatas = timeoutsFetcher.GetNextChunk(startSlice, out nextExpiredTimeout);

                foreach (var timeoutData in timeoutDatas)
                {
                    if (startSlice < timeoutData.Item2)
                    {
                        startSlice = timeoutData.Item2;
                    }


                    var dispatchRequest = ControlMessageFactory.Create(MessageIntentEnum.Send);

                    dispatchRequest.Headers["Timeout.Id"] = timeoutData.Item1;

                    dispatcher.Dispatch(dispatchRequest, new DispatchOptions(dispatcherAddress, new AtomicWithReceiveOperation(), new List<DeliveryConstraint>(), new ContextBag()));
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

        IQueryTimeouts timeoutsFetcher;
        IDispatchMessages dispatcher;
        string dispatcherAddress;
        RepeatedFailuresOverTimeCircuitBreaker circuitBreaker;
        object lockObject = new object();
        ManualResetEvent resetEvent = new ManualResetEvent(true);
        DateTime nextRetrieval = DateTime.UtcNow;
        volatile bool timeoutPushed;
        CancellationTokenSource tokenSource;

        static ILog Logger = LogManager.GetLogger<ExpiredTimeoutsPoller>();
    }
}