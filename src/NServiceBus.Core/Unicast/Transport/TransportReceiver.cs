namespace NServiceBus.Unicast.Transport
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Logging;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Transports;

    /// <summary>
    ///     Default implementation of a NServiceBus transport.
    /// </summary>
    public class TransportReceiver
    {
        internal TransportReceiver(string id, IBuilder builder, IPushMessages receiver, PushSettings pushSettings, PipelineBase<TransportReceiveContext> pipeline,PushRuntimeSettings pushRuntimeSettings)
        {
            Id = id;
            this.pipeline = pipeline;
            this.pushRuntimeSettings = pushRuntimeSettings;
            this.pushSettings = pushSettings;
            this.receiver = receiver;
            this.builder = builder;
        }


        /// <summary>
        /// Gets the ID of this pipeline.
        /// </summary>
        public string Id { get; private set; }

        Task InvokePipeline(PushContext pushContext)
        {
            using (var childBuilder = builder.CreateChildBuilder())
            {
                var configurer = (IConfigureComponents)childBuilder;
                var behaviorContextStacker = new BehaviorContextStacker(childBuilder);
                configurer.RegisterSingleton(behaviorContextStacker);
                configurer.ConfigureComponent<ContextualBus>(DependencyLifecycle.SingleInstance);

                var context = new TransportReceiveContext(pushContext.Message, behaviorContextStacker.Root);

                context.Merge(pushContext.Context);

                pipeline.Invoke(context);
            }

            return TaskEx.Completed;
        }
        
        /// <summary>
        ///     Starts the transport listening for messages on the given local address.
        /// </summary>
        public async Task Start()
        {
            if (isStarted)
            {
                throw new InvalidOperationException("The transport is already started");
            }

            Logger.DebugFormat("Pipeline {0} is starting receiver for queue {1}.", Id, pushSettings.InputQueue);

            var dequeueInfo = receiver.Init(InvokePipeline, pushSettings);
            pipeline.Initialize(new PipelineInfo(Id, dequeueInfo.PublicAddress));
            await pipeline.Warmup();

            receiver.Start(pushRuntimeSettings);
   
            isStarted = true;
        }

        /// <summary>
        ///     Stops the transport.
        /// </summary>
        public async Task Stop()
        {
            if (!isStarted)
            {
                return;
            }

            receiver.Stop();
            await pipeline.Cooldown();

            isStarted = false;
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