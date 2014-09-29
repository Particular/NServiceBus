namespace NServiceBus.Pipeline
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using Contexts;
    using ObjectBuilder;
    using Unicast;
    using Unicast.Behaviors;
    using Unicast.Messages;

    [Obsolete("This is a prototype API. May change in minor version releases.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
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

        public void PreparePhysicalMessagePipelineContext(TransportMessage message, bool messageHandlingDisabled)
        {
            contextStacker.Push(new ReceivePhysicalMessageContext(CurrentContext, message, messageHandlingDisabled));
        }

        public void InvokeReceivePhysicalMessagePipeline()
        {
            var context = contextStacker.Current as ReceivePhysicalMessageContext;

            if (context == null)
            {
                throw new InvalidOperationException("Can't invoke the receive pipeline when the current context is: " + contextStacker.Current.GetType().Name);
            }


            var pipeline = new BehaviorChain<ReceivePhysicalMessageContext>(pipelineBuilder.receivePhysicalMessageBehaviorList, context);


            pipeline.Invoke();
        }

        public void CompletePhysicalMessagePipelineContext()
        {
            contextStacker.Pop();
        }

        public void InvokeLogicalMessagePipeline(LogicalMessage message)
        {
            var context = new ReceiveLogicalMessageContext(CurrentContext, message);
            var pipeline = new BehaviorChain<ReceiveLogicalMessageContext>(pipelineBuilder.receiveLogicalMessageBehaviorList, context);

            Execute(pipeline, context);
        }

        public HandlerInvocationContext InvokeHandlerPipeline(MessageHandler handler)
        {
            var context = new HandlerInvocationContext(CurrentContext, handler);
            var pipeline = new BehaviorChain<HandlerInvocationContext>(pipelineBuilder.handlerInvocationBehaviorList,context);

            Execute(pipeline, context);

            return context;
        }

        public SendLogicalMessagesContext InvokeSendPipeline(SendOptions sendOptions, IEnumerable<LogicalMessage> messages)
        {
            var context = new SendLogicalMessagesContext(CurrentContext, sendOptions, messages);
            var pipeline = new BehaviorChain<SendLogicalMessagesContext>(pipelineBuilder.sendLogicalMessagesBehaviorList, context);

            Execute(pipeline, context);

            return context;
        }

        public SendLogicalMessageContext InvokeSendPipeline(SendOptions sendOptions, LogicalMessage message)
        {
            var context = new SendLogicalMessageContext(CurrentContext, sendOptions, message);
            var pipeline = new BehaviorChain<SendLogicalMessageContext>(pipelineBuilder.sendLogicalMessageBehaviorList, context);

            Execute(pipeline,context);

            return context;
        }

        public void InvokeSendPipeline(SendOptions sendOptions, TransportMessage physicalMessage)
        {
            var context = new SendPhysicalMessageContext(CurrentContext, sendOptions, physicalMessage);
            var pipeline = new BehaviorChain<SendPhysicalMessageContext>(pipelineBuilder.sendPhysicalMessageBehaviorList, context);

            Execute(pipeline, context);
        }

        public void InvokePipeline<TContext>(IEnumerable<Type> behaviours, TContext context) where TContext : BehaviorContext
        {
            var pipeline = new BehaviorChain<TContext>(behaviours, context);

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

                pipelineAction.Invoke();
            }
            finally
            {

                contextStacker.Pop();
            }
        }

    }
}