namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Logging;
    using Routing;
    using Timeout.Core;
    using Transport;
    using Unicast.Transport;

    class ExpiredTimeoutsPoller : IDisposable
    {
        public ExpiredTimeoutsPoller(IQueryTimeouts timeoutsFetcher, IDispatchMessages dispatcher, string dispatcherAddress, ICircuitBreaker circuitBreaker, Func<DateTime> currentTimeProvider)
        {
            this.timeoutsFetcher = timeoutsFetcher;
            this.dispatcher = dispatcher;
            this.dispatcherAddress = dispatcherAddress;
            this.circuitBreaker = circuitBreaker;
            this.currentTimeProvider = currentTimeProvider;

            var now = currentTimeProvider();
            startSlice = now.AddYears(-10);
            NextRetrieval = now;
        }

        public DateTime NextRetrieval { get; private set; }

        public void Dispose()
        {
            //Injected
        }

        public void Start()
        {
            tokenSource = new CancellationTokenSource();

            var token = tokenSource.Token;

            timeoutPollerTask = Task.Run(() => Poll(token));
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
                catch (OperationCanceledException)
                {
                    // ok, since the InnerPoll could observe the token
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
                await SpinOnce(cancellationToken).ConfigureAwait(false);
                await Task.Delay(NextRetrievalPollSleep, cancellationToken).ConfigureAwait(false);
            }
        }

        internal async Task SpinOnce(CancellationToken cancellationToken)
        {
            if (NextRetrieval > currentTimeProvider() || cancellationToken.IsCancellationRequested)
            {
                return;
            }

            Logger.DebugFormat("Polling for timeouts at {0}.", currentTimeProvider());
            var timeoutChunk = await timeoutsFetcher.GetNextChunk(startSlice).ConfigureAwait(false);

            foreach (var timeoutData in timeoutChunk.DueTimeouts)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                if (startSlice < timeoutData.DueTime)
                {
                    startSlice = timeoutData.DueTime;
                }

                var dispatchRequest = ControlMessageFactory.Create(MessageIntentEnum.Send);

                dispatchRequest.Headers["Timeout.Id"] = timeoutData.Id;

                var transportOperation = new TransportOperation(dispatchRequest, new UnicastAddressTag(dispatcherAddress));
                await dispatcher.Dispatch(new TransportOperations(transportOperation), new TransportTransaction(), new ContextBag()).ConfigureAwait(false);
            }

            lock (lockObject)
            {
                var nextTimeToQuery = timeoutChunk.NextTimeToQuery;

                // we cap the next retrieval to max 1 minute this will make sure that we trip the circuit breaker if we
                // loose connectivity to our storage. This will also make sure that timeouts added (during migration) direct to storage
                // will be picked up after at most 1 minute
                var maxNextRetrieval = currentTimeProvider() + MaxNextRetrievalDelay;

                NextRetrieval = nextTimeToQuery > maxNextRetrieval ? maxNextRetrieval : nextTimeToQuery;

                Logger.DebugFormat("Polling next retrieval is at {0}.", NextRetrieval.ToLocalTime());
            }

            circuitBreaker.Success();
        }

        ICircuitBreaker circuitBreaker;
        Func<DateTime> currentTimeProvider;
        IDispatchMessages dispatcher;
        string dispatcherAddress;
        object lockObject = new object();
        DateTime startSlice;
        Task timeoutPollerTask;

        IQueryTimeouts timeoutsFetcher;
        CancellationTokenSource tokenSource;

        static ILog Logger = LogManager.GetLogger<ExpiredTimeoutsPoller>();
        static readonly TimeSpan MaxNextRetrievalDelay = TimeSpan.FromMinutes(1);
        static readonly TimeSpan NextRetrievalPollSleep = TimeSpan.FromMilliseconds(1000);
    }
}