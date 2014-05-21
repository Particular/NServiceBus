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

            var outboxMessage = context.Get<OutboxMessage>();

            OutboxStorage.Store(outboxMessage.MessageId, outboxMessage.TransportOperations);
        }
    }
}