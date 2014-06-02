namespace NServiceBus.Features
{
    using NServiceBus.Outbox;
    using Pipeline;
    using Transports;

    /// <summary>
    /// Used to configure the outbox.
    /// </summary>
    public class Outbox : Feature
    {
        
        internal Outbox()
        {
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Pipeline.Register<OutboxDeduplicationRegistration>();
            context.Pipeline.Register<OutboxRecorderRegistration>();
            context.Pipeline.Replace(WellKnownBehavior.DispatchMessageToTransport, typeof(OutboxSendBehavior), "Sending behavior with a delay sending until all business transactions are committed to the outbox storage");

            //make the audit use the outbox as well
            if (context.Container.HasComponent<IAuditMessages>())
            {
                context.Container.ConfigureComponent<OutboxAwareAuditer>(DependencyLifecycle.InstancePerCall);
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
                InsertBefore(WellKnownBehavior.MutateIncomingTransportMessage);
                InsertAfter(WellKnownBehavior.UnitOfWork);
            }
        }
    }
}