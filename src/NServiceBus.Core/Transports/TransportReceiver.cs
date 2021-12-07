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
            INotificationSubscriptions<ConsecutiveFailuresArmed> consecutiveFailuresArmedNotification,
            INotificationSubscriptions<ConsecutiveFailuresDisarmed> consecutiveFailuresDisarmedNotification,
            ConsecutiveFailuresCircuitBreaker consecutiveFailuresCircuitBreaker)
        {
            Id = id;
            this.criticalError = criticalError;
            this.pushRuntimeSettings = pushRuntimeSettings;
            this.pipelineExecutor = pipelineExecutor;
            this.recoverabilityExecutor = recoverabilityExecutor;
            this.pushSettings = pushSettings;
            this.consecutiveFailuresArmedNotification = consecutiveFailuresArmedNotification;
            this.consecutiveFailuresDisarmedNotification = consecutiveFailuresDisarmedNotification;
            this.consecutiveFailuresCircuitBreaker = consecutiveFailuresCircuitBreaker;

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

        public Task Start()
        {
            if (state != TransportReceiverState.Stopped)
            {
                throw new InvalidOperationException("The transport is already started");
            }

            Logger.DebugFormat("Receiver {0} is starting, listening to queue {1}.", Id, pushSettings.InputQueue);

            receiver.Start(pushRuntimeSettings);

            state = TransportReceiverState.StartedRegular;

            return TaskEx.CompletedTask;
        }

        public async Task StartRateLimiting()
        {
            if (state == TransportReceiverState.StartedInRateLimitMode)
            {
                return;
            }

            if (await semaphoreSlim.WaitAsync(TimeSpan.FromMilliseconds(50)).ConfigureAwait(false))
            {
                try
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
                        await consecutiveFailuresArmedNotification.Raise(new ConsecutiveFailuresArmed()).ConfigureAwait(false);
                    }
                    catch (Exception exception)
                    {
                        Logger.WarnFormat("Failed to enter system outage mode: {0}", exception);
                    }
                }
                finally
                {
                    semaphoreSlim.Release();
                }
            }
        }

        public async Task<bool> StopRateLimiting()
        {
            if (state == TransportReceiverState.StartedRegular)
            {
                return true;
            }

            if (await semaphoreSlim.WaitAsync(TimeSpan.FromMilliseconds(50)).ConfigureAwait(false))
            {
                try
                {
                    if (state == TransportReceiverState.StartedRegular)
                    {
                        return true;
                    }

                    await StopAndDisposeReceiver().ConfigureAwait(false);
                    await Init().ConfigureAwait(false);
                    await Start().ConfigureAwait(false);

                    try
                    {
                        await consecutiveFailuresDisarmedNotification.Raise(new ConsecutiveFailuresDisarmed()).ConfigureAwait(false);
                    }
                    catch (Exception exception)
                    {
                        Logger.WarnFormat("Failed to stop system outage mode: {0}", exception);
                    }

                    return true;
                }
                finally
                {
                    semaphoreSlim.Release();
                }
            }
            return false;
        }

        public async Task Stop()
        {
            if (state == TransportReceiverState.Stopped)
            {
                return;
            }

            await semaphoreSlim.WaitAsync().ConfigureAwait(false);
            try
            {
                if (state == TransportReceiverState.Stopped)
                {
                    return;
                }

                try
                {
                    await StopAndDisposeReceiver().ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    Logger.Warn($"Receiver {Id} listening to queue {pushSettings.InputQueue} threw an exception on stopping.", exception);
                }
                finally
                {
                    state = TransportReceiverState.Stopped;
                }
            }
            finally
            {
                semaphoreSlim.Release();
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

        SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1);
        TransportReceiverState state;
        PushRuntimeSettings pushRuntimeSettings;
        IPipelineExecutor pipelineExecutor;
        RecoverabilityExecutor recoverabilityExecutor;
        PushSettings pushSettings;
        Func<IPushMessages> receiverFactory;
        IPushMessages receiver;
        readonly INotificationSubscriptions<ConsecutiveFailuresArmed> consecutiveFailuresArmedNotification;
        readonly INotificationSubscriptions<ConsecutiveFailuresDisarmed> consecutiveFailuresDisarmedNotification;
        readonly ConsecutiveFailuresCircuitBreaker consecutiveFailuresCircuitBreaker;

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