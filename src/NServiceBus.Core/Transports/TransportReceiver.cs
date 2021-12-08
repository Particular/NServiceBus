namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Logging;
    using Transport;

    class TransportReceiver
    {
        public TransportReceiver(
            string id,
            Func<IPushMessages> pushMessagesFactory,
            PushSettings pushSettings,
            PushRuntimeSettings pushRuntimeSettings,
            IPipelineExecutor pipelineExecutor,
            RecoverabilityExecutor recoverabilityExecutor,
            CriticalError criticalError,
            ConsecutiveFailuresConfiguration consecutiveFailuresConfiguration)
        {
            Id = id;
            this.criticalError = criticalError;
            this.consecutiveFailuresConfiguration = consecutiveFailuresConfiguration;
            this.pushRuntimeSettings = pushRuntimeSettings;
            this.pipelineExecutor = pipelineExecutor;
            this.recoverabilityExecutor = recoverabilityExecutor;
            this.pushSettings = pushSettings;

            consecutiveFailuresCircuitBreaker = consecutiveFailuresConfiguration.CreateCircuitBreaker(
                ticks =>
                {
                    SwitchToRateLimitMode(ticks);
                    return TaskEx.CompletedTask;
                },
                ticks =>
                {
                    SwitchBackToNormalMode(ticks);
                    return TaskEx.CompletedTask;
                });

            receiverFactory = pushMessagesFactory;
            rateLimitPushRuntimeSettings = new PushRuntimeSettings(1);
            rateLimitPushSettings = new PushSettings(this.pushSettings.InputQueue, this.pushSettings.ErrorQueue, false, this.pushSettings.RequiredTransactionMode);
        }

        public string Id { get; }

        public Task Init()
        {
            receiver = receiverFactory();
            return receiver.Init(InvokePipeline, c => recoverabilityExecutor.Invoke(c), criticalError, pushSettings);
        }

        async Task InvokePipeline(MessageContext c)
        {
            try
            {
                await pipelineExecutor.Invoke(c).ConfigureAwait(false);
                consecutiveFailuresCircuitBreaker.Success();
            }
            catch (Exception e)
            {
                await consecutiveFailuresCircuitBreaker.Failure(e).ConfigureAwait(false);
                throw;
            }
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

        void SwitchBackToNormalMode(long stateChangeTime)
        {
            // Switching to normal mode always takes precedence so we don't check the previous state change time.
            Interlocked.Exchange(ref lastStateChangeTime, stateChangeTime);

            endpointShouldBeRateLimited = false;
            resetEventReplacement.TrySetResult(true);
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

                resetEventReplacement = TaskCompletionSourceFactory.Create<bool>();

                while (!cancellationToken.IsCancellationRequested)
                {
                    var startRateLimiting = endpointShouldBeRateLimited;
                    try
                    {
                        if (startRateLimiting)
                        {
                            await StartRateLimiting().ConfigureAwait(false);
                        }
                        else
                        {
                            await StopRateLimiting().ConfigureAwait(false);
                        }

                        break;
                    }
                    catch (Exception exception)
                    {
                        Logger.WarnFormat("Could not switch to {0} mode. '{1}'", startRateLimiting ? "rate limit" : "normal", exception);
                        //Raise critical error
                    }
                }
            }
        }

        public Task Start()
        {
            if (state != TransportReceiverState.Stopped)
            {
                throw new InvalidOperationException("The transport is already started");
            }

            Logger.DebugFormat("Receiver {0} is starting, listening to queue {1}.", Id, pushSettings.InputQueue);

            receiver.Start(pushRuntimeSettings);

            state = TransportReceiverState.StartedRegular;


            if (consecutiveFailuresConfiguration.RateLimitSettings != null)
            {
                rateLimitTask = RateLimitLoop(rateLimitLoopCancellationToken.Token);
            }
            return TaskEx.CompletedTask;
        }

        async Task StartRateLimiting()
        {
            if (state == TransportReceiverState.StartedInRateLimitMode)
            {
                return;
            }

            await StopAndDisposeReceiver().ConfigureAwait(false);

            receiver = receiverFactory();

            await receiver.Init(InvokePipeline, c => recoverabilityExecutor.Invoke(c), criticalError, rateLimitPushSettings).ConfigureAwait(false);
            receiver.Start(rateLimitPushRuntimeSettings);

            state = TransportReceiverState.StartedInRateLimitMode;

            try
            {
                if (consecutiveFailuresConfiguration.RateLimitSettings.OnRateLimitStarted != null)
                {
                    await consecutiveFailuresConfiguration.RateLimitSettings.OnRateLimitStarted().ConfigureAwait(false);
                }
            }
            catch (Exception exception)
            {
                Logger.WarnFormat("Failed to enter system outage mode: {0}", exception);
            }
        }

        async Task StopRateLimiting()
        {
            if (state == TransportReceiverState.StartedRegular)
            {
                return;
            }

            await StopAndDisposeReceiver().ConfigureAwait(false);
            await Init().ConfigureAwait(false);
            await Start().ConfigureAwait(false);

            try
            {
                if (consecutiveFailuresConfiguration.RateLimitSettings.OnRateLimitEnded != null)
                {
                    await consecutiveFailuresConfiguration.RateLimitSettings.OnRateLimitEnded().ConfigureAwait(false);
                }
            }
            catch (Exception exception)
            {
                Logger.WarnFormat("Failed to stop system outage mode: {0}", exception);
            }
        }

        public async Task Stop()
        {
            if (state == TransportReceiverState.Stopped)
            {
                return;
            }

            try
            {
                //Wait for the loop to stop switching modes before stopping the receiver
                rateLimitLoopCancellationToken.Cancel();
                resetEventReplacement.TrySetResult(true);
                try
                {
                    if (rateLimitTask != null)
                    {
                        await rateLimitTask.ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                    //Ignore
                }

                await StopAndDisposeReceiver().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                Logger.Warn($"Receiver {Id} listening to queue {pushSettings.InputQueue} threw an exception on stopping.", exception);
            }
            finally
            {
                state = TransportReceiverState.Stopped;
                rateLimitLoopCancellationToken.Dispose();
            }
        }

        async Task StopAndDisposeReceiver()
        {
            await receiver.Stop().ConfigureAwait(false);
            (receiver as IDisposable)?.Dispose();
            receiver = null;
            state = TransportReceiverState.Stopped;
        }

        readonly CriticalError criticalError;
        readonly ConsecutiveFailuresConfiguration consecutiveFailuresConfiguration;

        TransportReceiverState state;
        PushRuntimeSettings pushRuntimeSettings;
        IPipelineExecutor pipelineExecutor;
        RecoverabilityExecutor recoverabilityExecutor;
        PushSettings pushSettings;
        Func<IPushMessages> receiverFactory;
        IPushMessages receiver;
        bool endpointShouldBeRateLimited;
        TaskCompletionSource<bool> resetEventReplacement = TaskCompletionSourceFactory.Create<bool>();
        readonly ConsecutiveFailuresCircuitBreaker consecutiveFailuresCircuitBreaker;
        readonly CancellationTokenSource rateLimitLoopCancellationToken = new CancellationTokenSource();
        Task rateLimitTask;
        long lastStateChangeTime = 0;

        static ILog Logger = LogManager.GetLogger<TransportReceiver>();
        PushRuntimeSettings rateLimitPushRuntimeSettings;
        PushSettings rateLimitPushSettings;
    }

    enum TransportReceiverState
    {
        Stopped,
        StartedInRateLimitMode,
        StartedRegular
    }
}