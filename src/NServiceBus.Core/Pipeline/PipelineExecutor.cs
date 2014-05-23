namespace NServiceBus.Pipeline
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Contexts;
    using ObjectBuilder;
    using Unicast;
    using Unicast.Messages;

    public class PipelineExecutor : IDisposable
    {
        public PipelineExecutor(IBuilder builder)
        {
            rootBuilder = builder;
            var pipelineBuilder = new PipelineBuilder();
            Incoming = pipelineBuilder.Incoming.AsReadOnly();
            Outgoing = pipelineBuilder.Outgoing.AsReadOnly();

            incomingBehaviors = Incoming.Select(r => r.BehaviorType);
            outgoingBehaviors = Outgoing.Select(r => r.BehaviorType);
        }

        public IList<RegisterBehavior> Incoming { get; private set; }
        public IList<RegisterBehavior> Outgoing { get; private set; }

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

        public void InvokePipeline<TContext>(IEnumerable<Type> behaviours, TContext context) where TContext : BehaviorContext
        {
            var pipeline = new BehaviorChain<TContext>(behaviours);

            Execute(pipeline, context);
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

                pipelineAction.Invoke(context);
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