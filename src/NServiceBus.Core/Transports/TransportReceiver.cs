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
            Func<IBuilder, PushContext, Task> onMessage)
        {
            Id = id;
            this.pushRuntimeSettings = pushRuntimeSettings;
            this.onMessage = onMessage;
            this.pushSettings = pushSettings;
            receiver = builder.Build<IPushMessages>();
            this.builder = builder;
        }

        public string Id { get; }

        public async Task Start()
        {
            if (isStarted)
            {
                throw new InvalidOperationException("The transport is already started");
            }

            Logger.DebugFormat("Pipeline {0} is starting receiver for queue {1}.", Id, pushSettings.InputQueue);

            await receiver.Init(c => onMessage(builder, c), builder.Build<CriticalError>(), pushSettings).ConfigureAwait(false);

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

        async Task InvokePipeline(PushContext pushContext)
        {
            var pipelineStartedAt = DateTime.UtcNow;

            using (var childBuilder = builder.CreateChildBuilder())
            {
                var rootContext = new RootContext(childBuilder, pipelineCache, eventAggregator);

                var message = new IncomingMessage(pushContext.MessageId, pushContext.Headers, pushContext.BodyStream);
                var context = new TransportReceiveContext(message, pushContext.TransportTransaction, pushContext.ReceiveCancellationTokenSource, rootContext);

                context.Extensions.Merge(pushContext.Context);

                await pipeline.Invoke(context).ConfigureAwait(false);

                //notifications are only relevant for the main pipeline since satellites pipelines are not exposed to user in any way
                if (isMainReceiver)
                {
                    await context.RaiseNotification(new ReceivePipelineCompleted(message, pipelineStartedAt, DateTime.UtcNow)).ConfigureAwait(false);
                }
            }
        }

        bool isMainReceiver;
        bool isStarted;
        IBuilder builder;
        PushRuntimeSettings pushRuntimeSettings;
        Func<IBuilder, PushContext, Task> onMessage;
        PushSettings pushSettings;
        IPushMessages receiver;

        static ILog Logger = LogManager.GetLogger<TransportReceiver>();
    }
}