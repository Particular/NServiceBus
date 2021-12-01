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
            INotificationSubscriptions<ThrottledModeStarted> throttledModeStartedNotification,
            INotificationSubscriptions<ThrottledModeEnded> throttledModeEndedNotification)
        {
            this.criticalError = criticalError;
            Id = id;
            this.pushRuntimeSettings = pushRuntimeSettings;
            this.pipelineExecutor = pipelineExecutor;
            this.recoverabilityExecutor = recoverabilityExecutor;
            this.pushSettings = pushSettings;
            this.throttledModeStartedNotification = throttledModeStartedNotification;
            this.throttledModeEndedNotification = throttledModeEndedNotification;

            receiverFactory = pushMessagesFactory;
            // TODO: Get this value from a setting
            throttledPushRuntimeSettings = new PushRuntimeSettings(1);
            throttledPushSettings = new PushSettings(this.pushSettings.InputQueue, this.pushSettings.ErrorQueue, false, this.pushSettings.RequiredTransactionMode);
        }

        public string Id { get; }

        public Task Init()
        {
            receiver = receiverFactory();
            return receiver.Init(c => pipelineExecutor.Invoke(c), c => recoverabilityExecutor.Invoke(c), criticalError, pushSettings);
        }

        //Start is called once before any message can be processed
        //Start throttling is called from a message handler

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

        public async Task StartThrottling()
        {
            if (state == TransportReceiverState.StartedInThrottledMode)
            {
                return;
            }

            if (await semaphoreSlim.WaitAsync(TimeSpan.FromMilliseconds(50)).ConfigureAwait(false))
            {
                try
                {
                    if (state == TransportReceiverState.StartedInThrottledMode)
                    {
                        return;
                    }
                    await StopAndDisposeReceiver().ConfigureAwait(false);

                    receiver = receiverFactory();

                    await receiver.Init(c => pipelineExecutor.Invoke(c), c => recoverabilityExecutor.Invoke(c), criticalError, throttledPushSettings).ConfigureAwait(false);
                    receiver.Start(throttledPushRuntimeSettings);

                    state = TransportReceiverState.StartedInThrottledMode;

                    try
                    {
                        await throttledModeStartedNotification.Raise(new ThrottledModeStarted()).ConfigureAwait(false);
                    }
                    catch (Exception)
                    {
                        // Log
                        // Swallow
                    }
                }
                finally
                {
                    semaphoreSlim.Release();
                }
            }
        }

        public async Task<bool> StopThrottling()
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
                        await throttledModeEndedNotification.Raise(new ThrottledModeEnded()).ConfigureAwait(false);
                    }
                    catch (Exception)
                    {
                        // Log
                        // Swallow
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
        readonly INotificationSubscriptions<ThrottledModeStarted> throttledModeStartedNotification;
        readonly INotificationSubscriptions<ThrottledModeEnded> throttledModeEndedNotification;

        static ILog Logger = LogManager.GetLogger<TransportReceiver>();
        PushRuntimeSettings throttledPushRuntimeSettings;
        PushSettings throttledPushSettings;
    }

    enum TransportReceiverState
    {
        Stopped,
        StartedInThrottledMode,
        StartedRegular
    }
}