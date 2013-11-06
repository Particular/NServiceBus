namespace NServiceBus.Pipeline
{
    using System;
    using Behaviors;
    using ObjectBuilder;
    using Sagas;
    using UnitOfWork;

    internal class PipelineFactory : IDisposable
    {
        public void InvokePhysicalMessagePipeline(IBuilder rootBuilder, TransportMessage msg, bool disableMessageHandling)
        {
            CurrentBuilder = rootBuilder;

            var pipeline = new BehaviorChain<PhysicalMessageContext>(rootBuilder);

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

            if (!disableMessageHandling)
            {
                pipeline.Add<ExtractLogicalMessagesBehavior>();
                pipeline.Add<CallbackInvocationBehavior>();
            }

            var context = new PhysicalMessageContext(this, CurrentContext, msg);


            contextStacker.Push(context);

            pipeline.Invoke(context);

            contextStacker.Pop();
        }


        public void InvokeLogicalMessagePipeline(LogicalMessage message)
        {

            var pipeline = new BehaviorChain<LogicalMessageContext>(CurrentBuilder);

            pipeline.Add<ApplyIncomingMessageMutatorsBehavior>();
            pipeline.Add<LoadHandlersBehavior>();


            var context = new LogicalMessageContext(this, CurrentContext, message);

            contextStacker.Push(context);

            pipeline.Invoke(context);

            contextStacker.Pop();
        }


        public void InvokeHandlerPipeline(MessageHandler handler)
        {
            var pipeline = new BehaviorChain<MessageHandlerContext>(CurrentBuilder);

        
            pipeline.Add<SagaPersistenceBehavior>();
            pipeline.Add<InvokeHandlersBehavior>();


            var context = new MessageHandlerContext(this,CurrentContext,handler);

            contextStacker.Push(context);

            pipeline.Invoke(context);

            contextStacker.Pop();
        }

        public BehaviorContext CurrentContext
        {
            get
            {
                return contextStacker.Current;
            }
        }

        public bool PipelineIsExecuting { get { return CurrentContext != null; } }
        public IBuilder CurrentBuilder { get; set; }

        public void Dispose()
        {
            //Injected
        }

        public void DisposeManaged()
        {
            contextStacker.Dispose();
        }

        BehaviorContextStacker contextStacker = new BehaviorContextStacker();

    }
}