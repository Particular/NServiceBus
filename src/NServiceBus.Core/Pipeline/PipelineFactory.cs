namespace NServiceBus.Pipeline
{
    using System;
    using Behaviors;
    using ObjectBuilder;
    using Sagas;
    using UnitOfWork;

    internal class PipelineFactory : IDisposable
    {
        internal IBuilder RootBuilder { get; set; }

        [ObsoleteEx(RemoveInVersion = "5.0")]
        internal void DisableLogicalMessageHandling()
        {
            messageHandlingDisabled = true;
        }

        internal void InvokePhysicalMessagePipeline(TransportMessage msg)
        {
            using (var childBuilder = RootBuilder.CreateChildBuilder())
            {
                var pipeline = new BehaviorChain<PhysicalMessageContext>(childBuilder);

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

                var context = new PhysicalMessageContext(childBuilder, null, msg);


                contextStacker.Push(context);

                pipeline.Invoke(context);

                contextStacker.Pop();
            }
        }

        internal void InvokeLogicalMessagePipeline(LogicalMessage message)
        {
            IBuilder builderToUse = null;

            if (PipelineIsExecuting)
            {
                builderToUse = CurrentContext.Builder;
            }
            else
            {
                //this will only happen when doing Bus.InMemory.Raise from a non message handler
                builderToUse = RootBuilder;
            }
            var pipeline = new BehaviorChain<LogicalMessageContext>(builderToUse);

            pipeline.Add<ApplyIncomingMessageMutatorsBehavior>();
            pipeline.Add<LoadHandlersBehavior>();


            var context = new LogicalMessageContext(builderToUse, CurrentContext, message);

            contextStacker.Push(context);

            pipeline.Invoke(context);

            contextStacker.Pop();
        }

        internal void InvokeHandlerPipeline(MessageHandler handler)
        {
            var pipeline = new BehaviorChain<MessageHandlerContext>(CurrentContext.Builder);



            pipeline.Add<SagaPersistenceBehavior>();

            //saga auditing will go here

            pipeline.Add<InvokeHandlersBehavior>();


            var context = new MessageHandlerContext(CurrentContext.Builder, CurrentContext, handler);

            contextStacker.Push(context);

            pipeline.Invoke(context);

            contextStacker.Pop();
        }


        internal BehaviorContext CurrentContext
        {
            get
            {
                return contextStacker.Current;
            }
        }

        internal bool PipelineIsExecuting { get { return CurrentContext != null; } }

        public void Dispose()
        {
            //Injected
        }

        public void DisposeManaged()
        {
            contextStacker.Dispose();
        }

        BehaviorContextStacker contextStacker = new BehaviorContextStacker();

        bool messageHandlingDisabled;
    }
}