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

            var messageId = context.PhysicalMessage.Id;
            var outboxMessage = context.Get<OutboxMessage>();

            OutboxStorage.Store(messageId, outboxMessage.TransportOperations);
        }
    }
}