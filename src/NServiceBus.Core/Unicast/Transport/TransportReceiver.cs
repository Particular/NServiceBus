namespace NServiceBus.Unicast.Transport
{
    using System;
    using System.Threading.Tasks;
    using Logging;
    using MessageInterfaces;
    using ObjectBuilder;
    using Pipeline;
    using Pipeline.Contexts;
    using Settings;
    using Transports;

    /// <summary>
    ///     Default implementation of a NServiceBus transport.
    /// </summary>
    public class TransportReceiver
    {
        internal TransportReceiver(string id, IBuilder builder, IPushMessages receiver, PushSettings pushSettings, PipelineBase<TransportReceiveContext> pipeline, PushRuntimeSettings pushRuntimeSettings)
        {
            Id = id;
            this.pipeline = pipeline;
            this.pushRuntimeSettings = pushRuntimeSettings;
            this.pushSettings = pushSettings;
            this.receiver = receiver;
            this.builder = builder;
        }

        /// <summary>
        ///     Gets the ID of this pipeline.
        /// </summary>
        public string Id { get; }

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

            receiver.Init(InvokePipeline, pushSettings);
            pipeline.Initialize(new PipelineInfo(Id, pushSettings.InputQueue));
            await pipeline.Warmup().ConfigureAwait(false);

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

            await receiver.Stop().ConfigureAwait(false);
            await pipeline.Cooldown().ConfigureAwait(false);

            isStarted = false;
        }

        async Task InvokePipeline(PushContext pushContext)
        {
            using (var childBuilder = builder.CreateChildBuilder())
            {
                var configurer = (IConfigureComponents) childBuilder;

                var context = new TransportReceiveContext(pushContext.Message, new RootContext(childBuilder));

                context.Merge(pushContext.Context);

                var contextStacker = new BehaviorContextStacker(context);
                var contextualBus = new ContextualBus(contextStacker, childBuilder.Build<IMessageMapper>(), childBuilder, childBuilder.Build<ReadOnlySettings>());
                configurer.ConfigureComponent<IBus>(c => contextualBus, DependencyLifecycle.SingleInstance);
                await pipeline.Invoke(contextStacker).ConfigureAwait(false);
            }
        }

        IBuilder builder;

        bool isStarted;
        PipelineBase<TransportReceiveContext> pipeline;
        PushRuntimeSettings pushRuntimeSettings;
        PushSettings pushSettings;
        IPushMessages receiver;

        static ILog Logger = LogManager.GetLogger<TransportReceiver>();
    }
}