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
            IEventAggregator eventAggregator,
            bool isMainReceiver)
        {
            Id = id;
            this.pipeline = pipeline;
            this.pipelineCache = pipelineCache;
            this.pushRuntimeSettings = pushRuntimeSettings;
            this.eventAggregator = eventAggregator;
            this.pushSettings = pushSettings;
            this.receiver = receiver;
            this.builder = builder;
            this.isMainReceiver = isMainReceiver;
        }

        public string Id { get; }

        public async Task Start()
        {
            if (isStarted)
            {
                throw new InvalidOperationException("The transport is already started");
            }

            Logger.DebugFormat("Pipeline {0} is starting receiver for queue {1}.", Id, pushSettings.InputQueue);

            await receiver.Init(InvokePipeline, InvokeErrorPipeline, builder.Build<CriticalError>(), pushSettings).ConfigureAwait(false);

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

        Task<bool> InvokeErrorPipeline(ErrorContext pushContext)
        {
            var dispatcher = builder.Build<IDispatchMessages>();

            return builder.Build<RecoveryActionExecutor>().RawInvoke(pushContext, dispatcher);
        }

        bool isMainReceiver;
        bool isStarted;
        IBuilder builder;
        IPipeline<ITransportReceiveContext> pipeline;
        IPipelineCache pipelineCache;
        PushRuntimeSettings pushRuntimeSettings;
        PushSettings pushSettings;
        IPushMessages receiver;
        IEventAggregator eventAggregator;

        static ILog Logger = LogManager.GetLogger<TransportReceiver>();
    }
}