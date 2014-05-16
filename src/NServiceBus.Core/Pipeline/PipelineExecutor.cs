namespace NServiceBus.Pipeline
{
    using System;
    using System.Collections.Generic;
    using Contexts;
    using ObjectBuilder;
    using Unicast;
    using Unicast.Messages;

    public class PipelineExecutor : IDisposable
    {
        IBuilder rootBuilder;
        PipelineBuilder pipelineBuilder;
        BehaviorContextStacker contextStacker = new BehaviorContextStacker();

        public PipelineExecutor(IBuilder builder, PipelineBuilder pipelineBuilder)
        {
            rootBuilder = builder;
            this.pipelineBuilder = pipelineBuilder;
        }

        public void PreparePhysicalMessagePipelineContext(TransportMessage message)
        {
            contextStacker.Push(new IncomingContext(CurrentContext, message));
        }

        public void InvokeReceivePhysicalMessagePipeline()
        {
            var context = contextStacker.Current as IncomingContext;

            if (context == null)
            {
                throw new InvalidOperationException("Can't invoke the receive pipeline when the current context is: " + contextStacker.Current.GetType().Name);
            }

            InvokePipeline(pipelineBuilder.receivePhysicalMessageBehaviorList, context);
        }

        public void CompletePhysicalMessagePipelineContext()
        {
            contextStacker.Pop();
        }

        public OutgoingContext InvokeSendPipeline(SendOptions sendOptions, LogicalMessage message)
        {
            var context = new OutgoingContext(CurrentContext, sendOptions, message);

            InvokePipeline(pipelineBuilder.sendLogicalMessageBehaviorList, context);

            return context;
        }

        public void InvokePipeline<TContext>(IEnumerable<Type> behaviours, TContext context) where TContext : BehaviorContext
        {
            var pipeline = new BehaviorChain<TContext>(behaviours);

            Execute(pipeline, context);
        }

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

        public void DisposeManaged()
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
    }
}