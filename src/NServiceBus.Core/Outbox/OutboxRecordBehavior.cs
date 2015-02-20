namespace NServiceBus
{
    using System;
    using NServiceBus.Outbox;
    using Pipeline;

    class OutboxRecordBehavior : PhysicalMessageProcessingStageBehavior
    {
        public IOutboxStorage OutboxStorage { get; set; }

        public override void Invoke(Context context, Action next)
        {
            next();
            
            if (context.handleCurrentMessageLaterWasCalled)
            {
                return;
            }

            var outboxMessage = context.Get<OutboxMessage>();

            OutboxStorage.Store(outboxMessage.MessageId, outboxMessage.TransportOperations);
        }

        public class OutboxRecorderRegistration : RegisterStep
        {
            public OutboxRecorderRegistration()
                : base("OutboxRecorder", typeof(OutboxRecordBehavior), "Records all action to the outbox storage")
            {
                InsertBefore(WellKnownStep.MutateIncomingTransportMessage);
                InsertAfter(WellKnownStep.ExecuteUnitOfWork);
            }
        }
    }
}