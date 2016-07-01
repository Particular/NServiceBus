namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Logging;
    using ObjectBuilder;
    using Transports;

    class TransportReceiver
    {
        public TransportReceiver(
            string id,
            IBuilder builder,
            PushSettings pushSettings,
            PushRuntimeSettings pushRuntimeSettings,
            Func<IBuilder, MessageContext, Task> onMessage,
            Func<IBuilder, ErrorContext, Task<bool>> onError)
        {
            Id = id;
            this.pushRuntimeSettings = pushRuntimeSettings;
            this.onMessage = onMessage;
            this.onError = onError;
            this.pushSettings = pushSettings;
            this.builder = builder;


            receiver = builder.Build<IPushMessages>();
        }

        public string Id { get; }

        public async Task Start()
        {
            if (isStarted)
            {
                throw new InvalidOperationException("The transport is already started");
            }

            Logger.DebugFormat("Pipeline {0} is starting receiver for queue {1}.", Id, pushSettings.InputQueue);

            await receiver.Init(c => onMessage(builder, c), c => onError(builder, c), (m, ex) =>
            {
                builder.Build<CriticalError>().Raise(m, ex);
                return TaskEx.CompletedTask;
            }, pushSettings).ConfigureAwait(false);

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
        IBuilder builder;
        PushRuntimeSettings pushRuntimeSettings;
        Func<IBuilder, MessageContext, Task> onMessage;
        Func<IBuilder, ErrorContext, Task<bool>> onError;
        PushSettings pushSettings;
        IPushMessages receiver;

        static ILog Logger = LogManager.GetLogger<TransportReceiver>();
    }
}