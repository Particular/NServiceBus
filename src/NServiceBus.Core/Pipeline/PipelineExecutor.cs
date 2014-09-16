namespace NServiceBus.Pipeline
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Contexts;
    using ObjectBuilder;
    using Settings;
    using Unicast;
    using Unicast.Messages;

    /// <summary>
    /// Orchestrates the execution of a pipeline.
    /// </summary>
    public class PipelineExecutor : IDisposable
    {
        /// <summary>
        /// Create a new instance of <see cref="PipelineExecutor"/>.
        /// </summary>
        /// <param name="settings">The settings to read data from.</param>
        /// <param name="builder">The builder.</param>
        public PipelineExecutor(ReadOnlySettings settings, IBuilder builder)
        {
            rootBuilder = builder;

            var pipelineBuilder = new PipelineBuilder(settings.Get<PipelineModifications>());
            Incoming = pipelineBuilder.Incoming.AsReadOnly();
            Outgoing = pipelineBuilder.Outgoing.AsReadOnly();

            incomingBehaviors = Incoming.Select(r => r.BehaviorType);
            outgoingBehaviors = Outgoing.Select(r => r.BehaviorType);
        }

        /// <summary>
        /// The list of incoming steps registered.
        /// </summary>
        public IList<RegisterStep> Incoming { get; private set; }
        
        /// <summary>
        /// The list of outgoing steps registered.
        /// </summary>
        public IList<RegisterStep> Outgoing { get; private set; }

        /// <summary>
        /// The current context being executed.
        /// </summary>
        public BehaviorContext CurrentContext
        {
            get
            {
                var current = contextStacker.Current;

                if (current != null)
                {
                    return current;
                }

                contextStacker.Push(new RootContext(rootBuilder));

                return contextStacker.Current;
            }
        }

        /// <summary>
        /// Invokes a chain of behaviors. 
        /// </summary>
        /// <typeparam name="TContext">The context to use.</typeparam>
        /// <param name="behaviors">The behaviors to execute in the specified order.</param>
        /// <param name="context">The context instance.</param>
        public void InvokePipeline<TContext>(IEnumerable<Type> behaviors, TContext context) where TContext : BehaviorContext
        {
            var pipeline = new BehaviorChain<TContext>(behaviors, context);

            Execute(pipeline, context);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            //Injected
        }

        internal void PreparePhysicalMessagePipelineContext(TransportMessage message)
        {
            contextStacker.Push(new IncomingContext(CurrentContext, message));
        }

        internal void InvokeReceivePhysicalMessagePipeline()
        {
            var context = contextStacker.Current as IncomingContext;

            if (context == null)
            {
                throw new InvalidOperationException("Can't invoke the receive pipeline when the current context is: " + contextStacker.Current.GetType().Name);
            }

            InvokePipeline(incomingBehaviors, context);
        }

        internal void CompletePhysicalMessagePipelineContext()
        {
            contextStacker.Pop();
        }

        internal OutgoingContext InvokeSendPipeline(DeliveryOptions deliveryOptions, LogicalMessage message)
        {
            var context = new OutgoingContext(CurrentContext, deliveryOptions, message);

            InvokePipeline(outgoingBehaviors, context);

            return context;
        }

        void DisposeManaged()
        {
            contextStacker.Dispose();
        }

        void Execute<T>(BehaviorChain<T> pipelineAction, T context) where T : BehaviorContext
        {
            try
            {
                contextStacker.Push(context);
                pipelineAction.Invoke();
            }
            finally
            {
                contextStacker.Pop();
            }
        }

        BehaviorContextStacker contextStacker = new BehaviorContextStacker();
        IBuilder rootBuilder;
        IEnumerable<Type> incomingBehaviors;
        IEnumerable<Type> outgoingBehaviors;
    }
}