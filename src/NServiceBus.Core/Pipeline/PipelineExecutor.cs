namespace NServiceBus.Pipeline
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Contexts;
    using Unicast;
    using Unicast.Messages;

    public class PipelineExecutor : IDisposable
    {
        public PipelineExecutor(Func<Type, object> builder, BehaviorContext rootContext)
        {
            this.builder = builder;
            this.rootContext = rootContext;
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

                contextStacker.Push(rootContext);
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

        internal OutgoingContext InvokeSendPipeline(SendOptions sendOptions, LogicalMessage message)
        {
            var context = new OutgoingContext(CurrentContext, sendOptions, message);

            InvokePipeline(outgoingBehaviors, context);

            return context;
        }

        public void InvokePipeline<TContext>(IEnumerable<Type> behaviours, TContext context) where TContext : BehaviorContext
        {
            var pipeline = new BehaviorChain<TContext>(builder, behaviours);

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
        Func<Type, object> builder;
        readonly BehaviorContext rootContext;
        IEnumerable<Type> incomingBehaviors;
        IEnumerable<Type> outgoingBehaviors;
    }
}