namespace NServiceBus.Pipeline
{
    using System;
    using Behaviors;
    using ObjectBuilder;
    using Sagas;
    using UnitOfWork;

    internal class PipelineFactory : IDisposable
    {
        public IBuilder RootBuilder { get; set; }

        [ObsoleteEx(RemoveInVersion = "5.0")]
        public void DisableLogicalMessageHandling()
        {
            messageHandlingDisabled = true;
        }

        public void InvokePhysicalMessagePipeline(TransportMessage msg)
        {
            var pipeline = new BehaviorChain<PhysicalMessageContext>();

            pipeline.Add<ChildContainerBehavior>();
            pipeline.Add<MessageHandlingLoggingBehavior>();

            if (ConfigureImpersonation.Impersonate)
            {
                pipeline.Add<ImpersonateSenderBehavior>();
            }

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

            var context = new PhysicalMessageContext(CurrentContext, msg);


            contextStacker.Push(context);

            pipeline.Invoke(context);

            contextStacker.Pop();

        }

        public void InvokeLogicalMessagePipeline(LogicalMessage message)
        {
            var pipeline = new BehaviorChain<LogicalMessageContext>();

            pipeline.Add<ApplyIncomingMessageMutatorsBehavior>();
            pipeline.Add<LoadHandlersBehavior>();


            var context = new LogicalMessageContext(CurrentContext, message);

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