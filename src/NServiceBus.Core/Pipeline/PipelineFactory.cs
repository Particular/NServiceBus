namespace NServiceBus.Pipeline
{
    using System;
    using System.Collections.Generic;
    using Audit;
    using Contexts;
    using DataBus;
    using MessageMutator;
    using NServiceBus.MessageMutator;
    using ObjectBuilder;
    using Sagas;
    using Unicast;
    using Unicast.Behaviors;
    using Unicast.Messages;
    using UnitOfWork;

    class PipelineFactory : IDisposable
    {
        public IBuilder RootBuilder { get; set; }

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

            var pipeline = new BehaviorChain<ReceivePhysicalMessageContext>();

            pipeline.Add<ChildContainerBehavior>();
            pipeline.Add<MessageHandlingLoggingBehavior>();
            pipeline.Add<ImpersonateSenderBehavior>();
            pipeline.Add<AuditBehavior>();
            pipeline.Add<ForwardBehavior>();
            pipeline.Add<UnitOfWorkBehavior>();
            pipeline.Add<ApplyIncomingTransportMessageMutatorsBehavior>();
            pipeline.Add<RaiseMessageReceivedBehavior>();
            pipeline.Add<ExtractLogicalMessagesBehavior>();
            pipeline.Add<CallbackInvocationBehavior>();
            pipeline.Add<ExecuteLogicalMessagesBehavior>();

            pipeline.Invoke(context);
        }

        public void CompletePhysicalMessagePipelineContext()
        {
            contextStacker.Pop();
        }

        public void InvokeLogicalMessagePipeline(LogicalMessage message)
        {
            var pipeline = new BehaviorChain<ReceiveLogicalMessageContext>();

            pipeline.Add<ApplyIncomingMessageMutatorsBehavior>();
            //todo: we'll make this optional as soon as we have a way to manipulate the pipeline
            pipeline.Add<DataBusReceiveBehavior>();
            pipeline.Add<LoadHandlersBehavior>();

            var context = new ReceiveLogicalMessageContext(CurrentContext, message);


            Execute(pipeline, context);
        }

        public HandlerInvocationContext InvokeHandlerPipeline(MessageHandler handler)
        {
            var pipeline = new BehaviorChain<HandlerInvocationContext>();

            pipeline.Add<SetCurrentMessageBeingHandledBehavior>();
            pipeline.Add<SagaPersistenceBehavior>();
            pipeline.Add<InvokeHandlersBehavior>();

            var context = new HandlerInvocationContext(CurrentContext, handler);

            Execute(pipeline, context);

            return context;
        }

        public SendLogicalMessagesContext InvokeSendPipeline(SendOptions sendOptions, IEnumerable<LogicalMessage> messages)
        {
            var pipeline = new BehaviorChain<SendLogicalMessagesContext>();

            pipeline.Add<MultiSendValidatorBehavior>();
            pipeline.Add<MultiMessageBehavior>();
            pipeline.Add<CreatePhysicalMessageBehavior>();

            var context = new SendLogicalMessagesContext(CurrentContext, sendOptions, messages);

            Execute(pipeline, context);

            return context;
        }

        public SendLogicalMessageContext InvokeSendPipeline(SendOptions sendOptions, LogicalMessage message)
        {
            var pipeline = new BehaviorChain<SendLogicalMessageContext>();

            pipeline.Add<SendValidatorBehavior>();
            pipeline.Add<SagaSendBehavior>();
            pipeline.Add<MutateOutgoingMessageBehavior>();

            //todo: we'll make this optional as soon as we have a way to manipulate the pipeline
            pipeline.Add<DataBusSendBehavior>();

            var context = new SendLogicalMessageContext(CurrentContext, sendOptions, message);

            Execute(pipeline,context);

            return context;
        }

        public void InvokeSendPipeline(SendOptions sendOptions, TransportMessage physicalMessage)
        {
            var pipeline = new BehaviorChain<SendPhysicalMessageContext>();

            pipeline.Add<SerializeMessagesBehavior>();
            pipeline.Add<MutateOutgoingPhysicalMessageBehavior>();
            pipeline.Add<DispatchMessageToTransportBehavior>();

            var context = new SendPhysicalMessageContext(CurrentContext, sendOptions, physicalMessage);

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

                contextStacker.Push(new RootContext(RootBuilder));

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

        BehaviorContextStacker contextStacker = new BehaviorContextStacker();
    }
}