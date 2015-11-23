namespace NServiceBus.Transport
{
    using System;
    using System.Threading.Tasks;
    using Logging;
    using Unicast.Transport;
    using ObjectBuilder;
    using Pipeline;
    using Pipeline.Contexts;
    using Transports;

    class TransportReceiver
    {
        public TransportReceiver(
            string id, 
            IBuilder builder, 
            IPushMessages receiver, 
            PushSettings pushSettings, 
            PipelineBase<TransportReceiveContext> pipeline, 
            PushRuntimeSettings pushRuntimeSettings)
        {
            Id = id;
            this.pipeline = pipeline;
            this.pushRuntimeSettings = pushRuntimeSettings;
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

            receiver.Init(InvokePipeline, pushSettings);
            pipeline.Initialize(new PipelineInfo(Id, pushSettings.InputQueue));
            await pipeline.Warmup().ConfigureAwait(false);

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
            await pipeline.Cooldown().ConfigureAwait(false);

            isStarted = false;
        }

        async Task InvokePipeline(PushContext pushContext)
        {
            using (var childBuilder = builder.CreateChildBuilder())
            {
                var context = new TransportReceiveContext(new IncomingMessage(pushContext.MessageId, pushContext.Headers, pushContext.BodyStream), new RootContext(childBuilder));
                context.Merge(pushContext.Context);
                await pipeline.Put(context).ConfigureAwait(false);
            }
        }

        static ILog Logger = LogManager.GetLogger<TransportReceiver>();

        IBuilder builder;
        PipelineBase<TransportReceiveContext> pipeline;
        PushRuntimeSettings pushRuntimeSettings;
        IPushMessages receiver;

        bool isStarted;
        PushSettings pushSettings;
    }
}