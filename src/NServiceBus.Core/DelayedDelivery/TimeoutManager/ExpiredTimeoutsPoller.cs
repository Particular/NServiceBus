namespace NServiceBus.DelayedDelivery.TimeoutManager
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.CircuitBreakers;
    using NServiceBus.Extensibility;
    using NServiceBus.Logging;
    using NServiceBus.Routing;
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

        async Task Poll(CancellationToken cancellationToken)
        {
            try
            {
                await InnerPoll(cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.Warn("Failed to fetch timeouts from the timeout storage", ex);
                circuitBreaker.Failure(ex);
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                await Poll(cancellationToken);
            }
        }

        async Task InnerPoll(CancellationToken cancellationToken)
        {
            var startSlice = DateTime.UtcNow.AddYears(-10);

            while (!cancellationToken.IsCancellationRequested)
            {
                if (nextRetrieval > DateTime.UtcNow)
                {
                    // ReSharper disable once MethodSupportsCancellation
                    await Task.Delay(1000).ConfigureAwait(false);
                    continue;
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

                    await dispatcher.Dispatch(dispatchRequest, new DispatchOptions(new DirectToTargetDestination(dispatcherAddress), new ContextBag()))
                        .ConfigureAwait(false);
                }

                lock (lockObject)
                {
                    //Check if nextRetrieval has been modified (This means that a push come in) and if it has check if it is earlier than nextExpiredTimeout time
                    if (!timeoutPushed)
                    {
                        nextRetrieval = timeoutChunk.NextTimeToQuery;
                    }
                    else if (timeoutChunk.NextTimeToQuery < nextRetrieval)
                    {
                        nextRetrieval = timeoutChunk.NextTimeToQuery;
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
        }

        IQueryTimeouts timeoutsFetcher;
        IDispatchMessages dispatcher;
        string dispatcherAddress;
        RepeatedFailuresOverTimeCircuitBreaker circuitBreaker;
        object lockObject = new object();
        Task timeoutPollerTask;
        DateTime nextRetrieval = DateTime.UtcNow;
        volatile bool timeoutPushed;
        CancellationTokenSource tokenSource;

        static ILog Logger = LogManager.GetLogger<ExpiredTimeoutsPoller>();
    }
}