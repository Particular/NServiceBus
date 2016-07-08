namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Logging;
    using Transports;

    class TransportReceiver
    {
        public TransportReceiver(
            string id,
            CriticalError criticalError,
            PushSettings pushSettings,
            PushRuntimeSettings pushRuntimeSettings,
            IPipelineInvoker pipelineInvoker,
            IPushMessages receiver)
        {
            Id = id;
            this.criticalError = criticalError;
            this.pushRuntimeSettings = pushRuntimeSettings;
            this.pipelineInvoker = pipelineInvoker;
            this.pushSettings = pushSettings;
            this.receiver = receiver;
        }

        public string Id { get; }

        public async Task Start()
        {
            if (isStarted)
            {
                throw new InvalidOperationException("The transport is already started");
            }

            Logger.DebugFormat("Pipeline {0} is starting receiver for queue {1}.", Id, pushSettings.InputQueue);

            await receiver.Init(c => pipelineInvoker.Invoke(c), criticalError, pushSettings).ConfigureAwait(false);

            receiver.Start(pushRuntimeSettings);

            isStarted = true;
        }

        public async Task Stop()
        {
            if (!isStarted)
            {
                return;
            }

            await receiver.Stop().ConfigureAwait(false);

            isStarted = false;
        }

        bool isStarted;
        CriticalError criticalError;
        PushRuntimeSettings pushRuntimeSettings;
        IPipelineInvoker pipelineInvoker;
        PushSettings pushSettings;
        IPushMessages receiver;

        static ILog Logger = LogManager.GetLogger<TransportReceiver>();
    }
}