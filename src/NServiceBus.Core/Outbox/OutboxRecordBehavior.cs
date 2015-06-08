namespace NServiceBus
{
    using System;
    using NServiceBus.Outbox;
    using Pipeline;

    class OutboxRecordBehavior : PhysicalMessageProcessingStageBehavior
    {
        public OutboxRecordBehavior(IStoreOutboxMessages outboxStorage)
        {
            this.storage = outboxStorage;
        }

        public override void Invoke(Context context, Action next)
        {
            next();
            
            if (context.handleCurrentMessageLaterWasCalled)
            {
                return;
            }

            var outboxMessage = context.Get<OutboxMessage>();

            storage.Store(outboxMessage.MessageId, outboxMessage.TransportOperations);
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

        IStoreOutboxMessages storage;
    }
}