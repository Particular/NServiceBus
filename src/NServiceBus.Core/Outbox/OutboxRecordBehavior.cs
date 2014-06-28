namespace NServiceBus.Outbox
{
    using System;
    using Pipeline;
    using Pipeline.Contexts;

    class OutboxRecordBehavior : IBehavior<IncomingContext>
    {
        public IOutboxStorage OutboxStorage { get; set; }

        public void Invoke(IncomingContext context, Action next)
        {
            next();
            
            if (context.handleCurrentMessageLaterWasCalled)
            {
                return;
            }

            var outboxMessage = context.Get<OutboxMessage>();

            OutboxStorage.Store(outboxMessage.MessageId, outboxMessage.TransportOperations);
        }

        public class OutboxRecorderRegistration : RegisterBehavior
        {
            public OutboxRecorderRegistration()
                : base(Pipeline.WellKnownStep.CreateCustom("OutboxRecorder"), typeof(OutboxRecordBehavior), "Records all action to the outbox storage")
            {
                InsertBefore(Pipeline.WellKnownStep.MutateIncomingTransportMessage);
                InsertAfter(Pipeline.WellKnownStep.ExecuteUnitOfWork);
            }
        }
    }
}