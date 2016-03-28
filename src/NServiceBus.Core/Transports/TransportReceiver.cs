namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Logging;
    using ObjectBuilder;
    using Pipeline;
    using Transports;

    class TransportReceiver
    {
        public TransportReceiver(
            string id,
            IBuilder builder,
            IPushMessages receiver,
            PushSettings pushSettings,
            IPipeline<ITransportReceiveContext> pipeline,
            IPipelineCache pipelineCache,
            PushRuntimeSettings pushRuntimeSettings, 
            IEventAggregator eventAggregator)
        {
            Id = id;
            this.pipeline = pipeline;
            this.pipelineCache = pipelineCache;
            this.pushRuntimeSettings = pushRuntimeSettings;
            this.eventAggregator = eventAggregator;
            this.pushSettings = pushSettings;
            this.receiver = receiver;
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

            await receiver.Init(InvokePipeline, builder.Build<CriticalError>(), pushSettings).ConfigureAwait(false);

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
            using (var childBuilder = builder.CreateChildBuilder())
            {
                var rootContext = new RootContext(childBuilder, pipelineCache, eventAggregator);
                var context = new TransportReceiveContext(new IncomingMessage(pushContext.MessageId, pushContext.Headers, pushContext.BodyStream), pushContext.TransportTransaction, pushContext.ReceiveCancellationTokenSource, rootContext);
                var startedAt = DateTime.UtcNow;

                var incomingMessage = new IncomingMessage(pushContext.MessageId, pushContext.Headers, pushContext.BodyStream);

                context.Extensions.Merge(pushContext.Context);

                await pipeline.Invoke(context).ConfigureAwait(false);

                await context.RaiseNotification(new ReceivePipelineCompleted(startedAt, DateTime.UtcNow, incomingMessage)).ConfigureAwait(false);
            }
        }

        IBuilder builder;

        bool isStarted;
        IPipeline<ITransportReceiveContext> pipeline;
        IPipelineCache pipelineCache;
        PushRuntimeSettings pushRuntimeSettings;
        PushSettings pushSettings;
        IPushMessages receiver;
        IEventAggregator eventAggregator;

        static ILog Logger = LogManager.GetLogger<TransportReceiver>();
    }
}