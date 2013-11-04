namespace NServiceBus.Pipeline
{
    using System;
    using Behaviors;
    using ObjectBuilder;
    using Sagas;
    using UnitOfWork;

    internal class PipelineFactory : IDisposable
    {
        public Action GetPhysicalMessagePipeline(IBuilder rootBuilder, TransportMessage msg, bool disableMessageHandling)
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
                pipeline.Add<ApplyIncomingMessageMutatorsBehavior>();
                pipeline.Add<CallbackInvocationBehavior>();
                pipeline.Add<LoadHandlersBehavior>();
                pipeline.Add<SagaPersistenceBehavior>();
                pipeline.Add<InvokeHandlersBehavior>();
            }

            return () =>
            {
                using (var context = new PhysicalMessageContext(this,msg))
                {
                    contextStacker.Push(context);

                    pipeline.Invoke(context);

                    contextStacker.Pop();
                }
            };
        }

        public Action GetHandlerPipeline(MessageHandler handler)
        {

            //var pipeline = new BehaviorChain(contextStacker.Current.Builder, contextStacker);

            return () =>
            {
                //using (var context = new BehaviorContext(contextStacker.Current.Builder, contextStacker))
                //{
                //    pipeline.Invoke(context);
                //}
            };
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