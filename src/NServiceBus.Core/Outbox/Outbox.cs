namespace NServiceBus.Features
{
    using NServiceBus.Outbox;
    using Pipeline;

    public class Outbox : Feature
    {
        public override void Initialize(Configure config)
        {
             
        }

        public class PipelineConfig : IWantToRunBeforeConfigurationIsFinalized
        {
            public void Run()
            {
                if (!IsEnabled<Outbox>())
                {
                    return;
                }

                Configure.Pipeline.Register<OutboxDeduplicationRegistration>();
                Configure.Pipeline.Register<OutboxRecorderRegistration>();
                Configure.Pipeline.Replace(WellKnownBehavior.DispatchMessageToTransport, typeof(OutboxSendBehavior), "Sending behavior with a delay sending until all business transactions are committed to the outbox storage");
            }
        }

        class OutboxDeduplicationRegistration : RegisterBehavior
        {
            public OutboxDeduplicationRegistration()
                : base("OutboxDeduplication", typeof(OutboxDeduplicationBehavior), "Deduplication for the outbox feature")
            {
                InsertAfter(WellKnownBehavior.ChildContainer);
                InsertBefore(WellKnownBehavior.UnitOfWork);
            }
        }

        class OutboxRecorderRegistration : RegisterBehavior
        {
            public OutboxRecorderRegistration()
                : base("OutboxRecorder", typeof(OutboxRecordBehavior), "Records all action to the outbox storage")
            {
                InsertBefore(WellKnownBehavior.IncomingTransportMessageMutators);
                InsertAfter(WellKnownBehavior.UnitOfWork);
            }
        }
    }
}
