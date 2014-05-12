﻿namespace NServiceBus.Pipeline
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

        public void PreparePhysicalMessagePipelineContext(TransportMessage message)
        {
            contextStacker.Push(new ReceivePhysicalMessageContext(CurrentContext, message));
        }

        public void InvokeReceivePhysicalMessagePipeline()
        {
            var context = contextStacker.Current as ReceivePhysicalMessageContext;

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

        public HandlerInvocationContext InvokeHandlerPipeline(MessageHandler handler)
        {
            var context = new HandlerInvocationContext(CurrentContext, handler);

            InvokePipeline(pipelineBuilder.handlerInvocationBehaviorList, context);

            return context;
        }

        public SendLogicalMessageContext InvokeSendPipeline(SendOptions sendOptions, LogicalMessage message)
        {
            var context = new SendLogicalMessageContext(CurrentContext, sendOptions, message);

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