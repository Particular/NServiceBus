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

        [ObsoleteEx(RemoveInVersion = "5.0")]
        public void DisableLogicalMessageHandling()
        {
            messageHandlingDisabled = true;
        }

        public void PreparePhysicalMessagePipelineContext(TransportMessage message)
        {
            contextStacker.Push(new IncomingPhysicalMessageContext(CurrentContext, message));
        }

        public void InvokeReceivePhysicalMessagePipeline()
        {
            var context = contextStacker.Current as IncomingPhysicalMessageContext;

            if(context == null)
            {
                throw new InvalidOperationException("Can't invoke the receive pipeline when the current context is: " + contextStacker.Current.GetType().Name);
            }

            var pipeline = new BehaviorChain<IncomingPhysicalMessageContext>();

            pipeline.Add<ChildContainerBehavior>();
            pipeline.Add<MessageHandlingLoggingBehavior>();
            pipeline.Add<ImpersonateSenderBehavior>();
            pipeline.Add<AuditBehavior>();
            pipeline.Add<ForwardBehavior>();
            pipeline.Add<UnitOfWorkBehavior>();
            pipeline.Add<ApplyIncomingTransportMessageMutatorsBehavior>();
            pipeline.Add<RaiseMessageReceivedBehavior>();

            if (!messageHandlingDisabled)
            {
                pipeline.Add<ExtractLogicalMessagesBehavior>();
                pipeline.Add<CallbackInvocationBehavior>();
            }

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

            contextStacker.Push(context);

            pipeline.Invoke(context);

            contextStacker.Pop();
        }

        public MessageHandlerContext InvokeHandlerPipeline(MessageHandler handler)
        {
            var pipeline = new BehaviorChain<MessageHandlerContext>();

            pipeline.Add<SagaPersistenceBehavior>();
            pipeline.Add<InvokeHandlersBehavior>();

            var context = new MessageHandlerContext(CurrentContext, handler);

            contextStacker.Push(context);

            pipeline.Invoke(context);

            contextStacker.Pop();

            return context;
        }

        public SendLogicalMessagesContext InvokeSendPipeline(SendOptions sendOptions, IEnumerable<LogicalMessage> messages)
        {
            var pipeline = new BehaviorChain<SendLogicalMessagesContext>();

            pipeline.Add<MultiSendValidatorBehavior>();
            pipeline.Add<MultiMessageBehavior>();
            pipeline.Add<CreatePhysicalMessageBehavior>();

            var context = new SendLogicalMessagesContext(CurrentContext, sendOptions, messages);

            contextStacker.Push(context);

            pipeline.Invoke(context);

            contextStacker.Pop();

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

            contextStacker.Push(context);

            pipeline.Invoke(context);

            contextStacker.Pop();

            return context;
        }

        public void InvokeSendPipeline(SendOptions sendOptions, TransportMessage physicalMessage)
        {
            var pipeline = new BehaviorChain<SendPhysicalMessageContext>();

            pipeline.Add<SerializeMessagesBehavior>();
            pipeline.Add<MutateOutgoingPhysicalMessageBehavior>();
            pipeline.Add<DispatchMessageToTransportBehavior>();

            var context = new SendPhysicalMessageContext(CurrentContext, sendOptions, physicalMessage);

            contextStacker.Push(context);

            pipeline.Invoke(context);

            contextStacker.Pop();
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

        BehaviorContextStacker contextStacker = new BehaviorContextStacker();

        bool messageHandlingDisabled;


    }
}