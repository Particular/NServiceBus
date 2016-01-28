namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Logging;
    using NServiceBus.Routing;
    using NServiceBus.Timeout.Core;
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Transport;

    class ExpiredTimeoutsPoller : IDisposable
    {
        public ExpiredTimeoutsPoller(IQueryTimeouts timeoutsFetcher, IDispatchMessages dispatcher, string dispatcherAddress, ICircuitBreaker circuitBreaker,
            TimeSpan? sleepTillNextRetrieval = null, TimeSpan? maxNextRetrievalDelay = null)
        {
            this.timeoutsFetcher = timeoutsFetcher;
            this.dispatcher = dispatcher;
            this.dispatcherAddress = dispatcherAddress;
            this.circuitBreaker = circuitBreaker;
            nextRetrievalPollSleep = sleepTillNextRetrieval ?? TimeSpan.FromMilliseconds(1000);
            this.maxNextRetrievalDelay = maxNextRetrievalDelay ?? DefaultMaxNextRetrievalDelay;
            startSlice = DateTime.UtcNow.AddYears(-10);
        }

        public DateTime NextRetrieval { get; set; } = DateTime.UtcNow;

        public void Dispose()
        {
            //Injected
        }

        public void Start()
        {
            tokenSource = new CancellationTokenSource();

            var token = tokenSource.Token;

            timeoutPollerTask = Task.Factory
                .StartNew(() => Poll(token), TaskCreationOptions.LongRunning)
                .Unwrap();
        }

        public Task Stop()
        {
            tokenSource.Cancel();
            return timeoutPollerTask;
        }

        public void NewTimeoutRegistered(DateTime expiryTime)
        {
            lock (lockObject)
            {
                if (NextRetrieval > expiryTime)
                {
                    NextRetrieval = expiryTime;
                }
                timeoutPushed = true;
            }
        }

        async Task Poll(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await InnerPoll(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.Warn("Failed to fetch timeouts from the timeout storage", ex);
                    await circuitBreaker.Failure(ex).ConfigureAwait(false);
                }
            }
        }

        async Task InnerPoll(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await SpinOnce().ConfigureAwait(false);
            }
        }

        internal async Task SpinOnce()
        {
            if (NextRetrieval > DateTime.UtcNow)
            {
                await Task.Delay(nextRetrievalPollSleep).ConfigureAwait(false);
                return;
            }

            Logger.DebugFormat("Polling for timeouts at {0}.", DateTime.Now);

            var timeoutChunk = await timeoutsFetcher.GetNextChunk(startSlice).ConfigureAwait(false);

            foreach (var timeoutData in timeoutChunk.DueTimeouts)
            {
                if (startSlice < timeoutData.DueTime)
                {
                    startSlice = timeoutData.DueTime;
                }

                var dispatchRequest = ControlMessageFactory.Create(MessageIntentEnum.Send);

                dispatchRequest.Headers["Timeout.Id"] = timeoutData.Id;

                var transportOperation = new TransportOperation(dispatchRequest, new UnicastAddressTag(dispatcherAddress));
                await dispatcher.Dispatch(new TransportOperations(transportOperation), new ContextBag()).ConfigureAwait(false);
            }

            lock (lockObject)
            {
                //Check if nextRetrieval has been modified (This means that a push come in) and if it has check if it is earlier than nextExpiredTimeout time
                if (!timeoutPushed)
                {
                    NextRetrieval = timeoutChunk.NextTimeToQuery;
                }
                else if (timeoutChunk.NextTimeToQuery < NextRetrieval)
                {
                    NextRetrieval = timeoutChunk.NextTimeToQuery;
                }

                timeoutPushed = false;
            }

            // we cap the next retrieval to max 1 minute this will make sure that we trip the circuit breaker if we
            // loose connectivity to our storage. This will also make sure that timeouts added (during migration) direct to storage
            // will be picked up after at most 1 minute
            var maxNextRetrieval = DateTime.UtcNow + maxNextRetrievalDelay;

            if (NextRetrieval > maxNextRetrieval)
            {
                NextRetrieval = maxNextRetrieval;
            }

            Logger.DebugFormat("Polling next retrieval is at {0}.", NextRetrieval.ToLocalTime());
            circuitBreaker.Success();
        }

        ICircuitBreaker circuitBreaker;
        IDispatchMessages dispatcher;
        string dispatcherAddress;
        object lockObject = new object();
        TimeSpan maxNextRetrievalDelay;
        TimeSpan nextRetrievalPollSleep;
        DateTime startSlice;
        Task timeoutPollerTask;
        volatile bool timeoutPushed;

        IQueryTimeouts timeoutsFetcher;
        CancellationTokenSource tokenSource;

        static ILog Logger = LogManager.GetLogger<ExpiredTimeoutsPoller>();
        static readonly TimeSpan DefaultMaxNextRetrievalDelay = TimeSpan.FromMinutes(1);
    }
}