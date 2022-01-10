namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Logging;
    using NServiceBus.Transport;

    class WrappedMessageReceiver : IMessageReceiver
    {
        public WrappedMessageReceiver(ConsecutiveFailuresConfiguration consecutiveFailuresConfiguration, IMessageReceiver baseReceiver)
        {
            this.baseReceiver = baseReceiver;
            this.consecutiveFailuresConfiguration = consecutiveFailuresConfiguration;
        }

        public async Task WrappedInvoke(MessageContext messageContext, CancellationToken cancellationToken = default)
        {
            await wrappedOnMessage(messageContext, cancellationToken).ConfigureAwait(false);
            await consecutiveFailuresCircuitBreaker.Success(cancellationToken).ConfigureAwait(false);
        }

        public async Task<ErrorHandleResult> WrappedOnError(ErrorContext errorContext, CancellationToken cancellationToken = default)
        {
            await consecutiveFailuresCircuitBreaker.Failure(cancellationToken).ConfigureAwait(false);

            return await wrappedOnError(errorContext, cancellationToken).ConfigureAwait(false);
        }

        public ISubscriptionManager Subscriptions => baseReceiver.Subscriptions;
        public string Id => baseReceiver.Id;
        public string ReceiveAddress => baseReceiver.ReceiveAddress;

        public Task ChangeConcurrency(PushRuntimeSettings limitations, CancellationToken cancellationToken = default)
        {
            return baseReceiver.ChangeConcurrency(limitations, cancellationToken);
        }

        public Task StartReceive(CancellationToken cancellationToken = default)
        {
            rateLimitTask = RateLimitLoop(rateLimitLoopCancellationToken.Token);

            return baseReceiver.StartReceive(cancellationToken);
        }

        public async Task StopReceive(CancellationToken cancellationToken = default)
        {
            // Todo: can stop receive be called without cancellationToken being cancelled?
            try
            {
                if (rateLimitTask != null)
                {
                    rateLimitLoopCancellationToken.Cancel();
                    resetEventReplacement.TrySetResult(true);
                    await rateLimitTask.ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                //Ignore
            }

            await baseReceiver.StopReceive(cancellationToken).ConfigureAwait(false);
        }

        public Task Initialize(PushRuntimeSettings limitations, OnMessage onMessage, OnError onError, CancellationToken cancellationToken = default)
        {
            wrappedOnMessage = onMessage;
            wrappedOnError = onError;
            originalLimitations = limitations;

            consecutiveFailuresCircuitBreaker = consecutiveFailuresConfiguration.CreateCircuitBreaker((ticks, cancellationToken) =>
            {
                SwitchToRateLimitMode(ticks);
                return Task.CompletedTask;
            }, (ticks, cancellationToken) =>
            {
                SwitchBackToNormalMode(ticks);
                return Task.CompletedTask;
            });

            return baseReceiver.Initialize(originalLimitations, WrappedInvoke, WrappedOnError, cancellationToken);
        }

        async Task RateLimitLoop(CancellationToken cancellationToken)
        {
            //We want all the pumps to be running all the time in the desired mode until we call stop
            //We want to make sure that StopRateLimit signal is not lost
            //The circuit breaker ensures that if StartRateLimit has been called that eventually StopRateLimit is going to be called unless the endpoint stop
            while (!cancellationToken.IsCancellationRequested)
            {
                await resetEventReplacement.Task.ConfigureAwait(false);
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                resetEventReplacement = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

                while (!cancellationToken.IsCancellationRequested)
                {
                    var startRateLimiting = endpointShouldBeRateLimited;
                    try
                    {
                        if (startRateLimiting)
                        {
                            await StartRateLimiting(cancellationToken).ConfigureAwait(false);
                        }
                        else
                        {
                            await StopRateLimiting(cancellationToken).ConfigureAwait(false);
                        }

                        break;
                    }
                    catch (Exception exception) when (!(exception is OperationCanceledException) || !cancellationToken.IsCancellationRequested)
                    {
                        Logger.WarnFormat("Could not switch to {0} mode. '{1}'", startRateLimiting ? "rate limit" : "normal", exception);
                        //Raise critical error
                    }
                }
            }
        }

        async Task StartRateLimiting(CancellationToken cancellationToken)
        {
            try
            {
                await ChangeConcurrency(RateLimitedRuntimeSettings, cancellationToken).ConfigureAwait(false);

                if (consecutiveFailuresConfiguration.RateLimitSettings.OnRateLimitStarted != null)
                {
                    await consecutiveFailuresConfiguration.RateLimitSettings.OnRateLimitStarted(cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception exception) when (!(exception is OperationCanceledException) || !cancellationToken.IsCancellationRequested)
            {
                Logger.WarnFormat("Failed to enter system outage mode: {0}", exception);
            }
        }

        async Task StopRateLimiting(CancellationToken cancellationToken)
        {
            try
            {
                await ChangeConcurrency(originalLimitations, cancellationToken).ConfigureAwait(false);

                if (consecutiveFailuresConfiguration.RateLimitSettings.OnRateLimitEnded != null)
                {
                    await consecutiveFailuresConfiguration.RateLimitSettings.OnRateLimitEnded(cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception exception) when (!(exception is OperationCanceledException) || !cancellationToken.IsCancellationRequested)
            {
                Logger.WarnFormat("Failed to stop system outage mode: {0}", exception);
            }
        }

        void SwitchBackToNormalMode(long stateChangeTime)
        {
            // Switching to normal mode always takes precedence so we don't check the previous state change time.
            Interlocked.Exchange(ref lastStateChangeTime, stateChangeTime);

            endpointShouldBeRateLimited = false;
            resetEventReplacement.TrySetResult(true);
        }

        void SwitchToRateLimitMode(long stateChangeTime)
        {
            // Only change states if the trigger time is greater than the last time the state change was triggered.
            // This prevents race conditions where switching to rate limited mode is executed after the
            // system has repaired and should be in normal mode.
            if (stateChangeTime >= Interlocked.Read(ref lastStateChangeTime))
            {
                Interlocked.Exchange(ref lastStateChangeTime, stateChangeTime);

                endpointShouldBeRateLimited = true;
                resetEventReplacement.TrySetResult(true);
            }
        }

        static ILog Logger = LogManager.GetLogger<WrappedMessageReceiver>();
        static PushRuntimeSettings RateLimitedRuntimeSettings = new PushRuntimeSettings(1);

        readonly IMessageReceiver baseReceiver;
        readonly ConsecutiveFailuresConfiguration consecutiveFailuresConfiguration;
        ConsecutiveFailuresCircuitBreaker consecutiveFailuresCircuitBreaker;
        TaskCompletionSource<bool> resetEventReplacement = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        Task rateLimitTask;
        readonly CancellationTokenSource rateLimitLoopCancellationToken = new CancellationTokenSource();
        OnMessage wrappedOnMessage;
        OnError wrappedOnError;
        PushRuntimeSettings originalLimitations;

        bool endpointShouldBeRateLimited;
        long lastStateChangeTime = 0;
    }
}
