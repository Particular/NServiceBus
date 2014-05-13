namespace NServiceBus.Outbox
{
    using System;
    using Pipeline;
    using Pipeline.Contexts;

    public class OutboxRecordBehavior : IBehavior<ReceivePhysicalMessageContext>
    {
        public IOutboxStorage OutboxStorage { get; set; }

        public void Invoke(ReceivePhysicalMessageContext context, Action next)
        {
            next();

            var messageId = context.PhysicalMessage.Id;
            
            OutboxMessage outboxMessage;

            if (context.TryGet(messageId, out outboxMessage))
            {
                OutboxStorage.Store(messageId, outboxMessage.TransportOperations);
            }
        }
    }
}