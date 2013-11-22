namespace NServiceBus.Persistence.InMemory.Outbox
{
    using NServiceBus.Outbox;

    class InMemoryOutboxStorage:IOutboxStorage
    {
        public OutboxMessage Get(string messageId)
        {
            return null;
        }

        public void Store(OutboxMessage outboxMessage)
        {
        }

        public void SetAsDispatched(OutboxMessage outboxMessage)
        {
        }
    }
}