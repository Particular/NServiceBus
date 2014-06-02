namespace NServiceBus.Features
{
    using NServiceBus.Outbox;
    using Pipeline;
    using Transports;
    using Unicast;

    /// <summary>
    /// Outbox
    /// </summary>
    public class Outbox : Feature
    {
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

    class OutboxAwareAuditer:IAuditMessages
    {
        public DefaultMessageAuditer DefaultMessageAuditer { get; set; }

        public PipelineExecutor PipelineExecutor { get; set; }

        public void Audit( SendOptions sendOptions, TransportMessage message)
        {
            var context = PipelineExecutor.CurrentContext;

            OutboxMessage currentOutboxMessage;

            if (context.TryGet(out currentOutboxMessage))
            {
                currentOutboxMessage.TransportOperations.Add(new TransportOperation(message.Id, sendOptions.ToTransportOperationOptions(true), message.Body, message.Headers));
            }
            else
            {
                DefaultMessageAuditer.Audit(sendOptions,message);
            }
        }
    }
}