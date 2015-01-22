namespace NServiceBus.Unicast.Transport
{
    using System;
    using NServiceBus.Logging;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Transport.Monitoring;

    //Shared thread pool dispatcher
    //Individual thread pool dispatcher
    //Shared throughput limit
    //Individual throughput limit

    /// <summary>
    ///     Default implementation of a NServiceBus transport.
    /// </summary>
    public class TransportReceiver : IDisposable, IObserver<MessageAvailable>
    {
        internal TransportReceiver(string id, IBuilder builder, IDequeueMessages receiver, DequeueSettings dequeueSettings, PipelineExecutor pipelineExecutor, IExecutor executor)
        {
            this.id = id;
            this.builder = builder;
            this.pipelineExecutor = pipelineExecutor;
            this.executor = executor;
            this.dequeueSettings = dequeueSettings;
            this.receiver = receiver;
        }


        /// <summary>
        /// Gets the ID of this pipeline
        /// </summary>
        public string Id
        {
            get { return id; }
        }

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        void IDisposable.Dispose()
        {
            //Injected at compile time
        }

        void IObserver<MessageAvailable>.OnNext(MessageAvailable value)
        {
            InvokePipeline(value);
            //TODO: I think I need to do some logging here, if a behavior can't be instantiated no error message is shown!
            //todo: I want to start a new instance of a pipeline and not use thread statics 
            //todo: Szymon: removing the try as it impedes testing
        }

        void InvokePipeline(MessageAvailable messageAvailable)
        {
            var context = new IncomingContext(new RootContext(builder));

            messageAvailable.InitializeContext(context);
            context.SetPublicReceiveAddress(messageAvailable.PublicReceiveAddress);
            context.Set(currentReceivePerformanceDiagnostics);
            SetContext(context);

            executor.Execute(Id, () => pipelineExecutor.InvokeReceivePipeline(context));
        }

        /// <summary>
        /// Sets the context for processing an incoming message.
        /// </summary>
        /// <param name="context"></param>
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
        public virtual void Start()
        {
            if (isStarted)
            {
                throw new InvalidOperationException("The transport is already started");
            }

            InitializePerformanceCounters(dequeueSettings.QueueName);

            Logger.DebugFormat("Pipeline {0} is starting receiver for queue {0}.", Id, dequeueSettings.QueueName);

            receiver.Init(dequeueSettings);

            StartReceiver();

            isStarted = true;
        }

        /// <summary>
        ///     Stops the transport.
        /// </summary>
        public virtual void Stop()
        {
            InnerStop();
        }

        void InitializePerformanceCounters(string queueName)
        {
            currentReceivePerformanceDiagnostics = new ReceivePerformanceDiagnostics(queueName);
        }

        void StartReceiver()
        {
            receiver.Subscribe(this);
            receiver.Start();
        }

        /// <summary>
        /// </summary>
        protected virtual void InnerStop()
        {
            if (!isStarted)
            {
                return;
            }

            receiver.Stop();

            isStarted = false;
        }

        void DisposeManaged()
        {
            InnerStop();

            if (currentReceivePerformanceDiagnostics != null)
            {
                currentReceivePerformanceDiagnostics.Dispose();
            }
        }

        /// <summary>
        /// </summary>
        public override string ToString()
        {
            return "Pipeline " + id;
        }

        static ILog Logger = LogManager.GetLogger<TransportReceiver>();



        readonly string id;
        readonly IBuilder builder;
        readonly PipelineExecutor pipelineExecutor;
        readonly IExecutor executor;
        readonly IDequeueMessages receiver;

        ReceivePerformanceDiagnostics currentReceivePerformanceDiagnostics;

        bool isStarted;
        readonly DequeueSettings dequeueSettings;
    }
}