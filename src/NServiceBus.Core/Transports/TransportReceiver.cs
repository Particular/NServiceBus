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
            IPushMessages pushMessages,
            PushSettings pushSettings,
            PushRuntimeSettings pushRuntimeSettings,
            IPipelineExecutor pipelineExecutor,
            RecoverabilityExecutor recoverabilityExecutor,
            CriticalError criticalError)
        {
            this.criticalError = criticalError;
            Id = id;
            this.pushRuntimeSettings = pushRuntimeSettings;
            this.pipelineExecutor = pipelineExecutor;
            this.recoverabilityExecutor = recoverabilityExecutor;
            this.pushSettings = pushSettings;

            receiver = pushMessages;
        }

        public string Id { get; }

        public Task Init(CancellationToken cancellationToken)
        {
            return receiver.Init((c,ct) => pipelineExecutor.Invoke(c, ct), (c,ct) => recoverabilityExecutor.Invoke(c, ct), criticalError, pushSettings, cancellationToken);
        }

        public Task Start(CancellationToken cancellationToken)
        {
            if (isStarted)
            {
                throw new InvalidOperationException("The transport is already started");
            }

            Logger.DebugFormat("Receiver {0} is starting, listening to queue {1}.", Id, pushSettings.InputQueue);

            receiver.Start(pushRuntimeSettings, cancellationToken);

            isStarted = true;

            return Task.FromResult(0);
        }

        public async Task Stop(CancellationToken cancellationToken)
        {
            if (!isStarted)
            {
                return;
            }

            try
            {
                await receiver.Stop(cancellationToken).ConfigureAwait(false);
                (receiver as IDisposable)?.Dispose();
            }
            catch (Exception exception)
            {
                Logger.Warn($"Receiver {Id} listening to queue {pushSettings.InputQueue} threw an exception on stopping.", exception);
            }
            finally
            {
                isStarted = false;
            }
        }

        readonly CriticalError criticalError;

        bool isStarted;
        PushRuntimeSettings pushRuntimeSettings;
        IPipelineExecutor pipelineExecutor;
        RecoverabilityExecutor recoverabilityExecutor;
        PushSettings pushSettings;
        IPushMessages receiver;

        static ILog Logger = LogManager.GetLogger<TransportReceiver>();
    }
}