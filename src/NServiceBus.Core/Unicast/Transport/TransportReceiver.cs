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
    public class TransportReceiver : IObserver<MessageAvailable>
    {
        internal TransportReceiver(string id, IBuilder builder, IDequeueMessages receiver, DequeueSettings dequeueSettings, PipelineBase<IncomingContext> pipeline, IExecutor executor)
        {
            Id = id;
            this.pipeline = pipeline;
            this.executor = executor;
            this.dequeueSettings = dequeueSettings;
            this.receiver = receiver;
            this.builder = builder;
        }


        /// <summary>
        /// Gets the ID of this pipeline.
        /// </summary>
        public string Id { get; private set; }


        void IObserver<MessageAvailable>.OnNext(MessageAvailable value)
        {
            InvokePipeline(value);
            //TODO: I think I need to do some logging here, if a behavior can't be instantiated no error message is shown!
            //todo: I want to start a new instance of a pipeline and not use thread statics 
            //todo: Szymon: removing the try as it impedes testing
        }

        void InvokePipeline(MessageAvailable messageAvailable)
        {
            executor.Execute(Id, () =>
            {
                try
                {
                    using (var childBuilder = builder.CreateChildBuilder())
                    {
                        var configurer = (IConfigureComponents)childBuilder;
                        var behaviorContextStacker = new BehaviorContextStacker(childBuilder);
                        configurer.RegisterSingleton(behaviorContextStacker);
                        configurer.ConfigureComponent<ContextualBus>(DependencyLifecycle.SingleInstance);

                        var context = new IncomingContext(behaviorContextStacker.Root);
                        messageAvailable.InitializeContext(context);
                        SetContext(context);

                        pipeline.Invoke(context);
                    }
                }
                catch (MessageProcessingAbortedException)
                {
                    //We swallow this one because it is used to signal aborting of processing.
                }
            });
        }

        /// <summary>
        /// Sets the context for processing an incoming message.
        /// </summary>
        protected virtual void SetContext(IncomingContext context)
        {
        }

        void IObserver<MessageAvailable>.OnError(Exception error)
        {
        }

        void IObserver<MessageAvailable>.OnCompleted()
        {
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

            Logger.DebugFormat("Pipeline {0} is starting receiver for queue {0}.", Id, dequeueSettings.QueueName);

            var dequeueInfo = receiver.Init(dequeueSettings);
            pipeline.Initialize(new PipelineInfo(Id, dequeueInfo.PublicAddress));
            await pipeline.Warmup();

            StartReceiver();

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

        void StartReceiver()
        {
            receiver.Subscribe(this);
            receiver.Start();
        }

        static ILog Logger = LogManager.GetLogger<TransportReceiver>();

        IBuilder builder;
        PipelineBase<IncomingContext> pipeline;
        IExecutor executor;
        IDequeueMessages receiver;

        bool isStarted;
        DequeueSettings dequeueSettings;
    }
}