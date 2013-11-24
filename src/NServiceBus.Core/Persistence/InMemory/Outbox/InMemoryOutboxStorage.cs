namespace NServiceBus.Persistence.InMemory.Outbox
{
    using System.Collections.Concurrent;
    using NServiceBus.Outbox;

    class InMemoryOutboxStorage:IOutboxStorage
    {
        public OutboxMessage Get(string messageId)
        {
            OutboxMessage message;

            storage.TryGetValue(messageId, out message);
            return message;
        }

        public void Store(OutboxMessage outboxMessage)
        {
            if (!storage.TryAdd(outboxMessage.Id, outboxMessage))
            {
                throw new ConcurrencyException(string.Format("Outbox message with id: '{0}' is already present in storage",outboxMessage.Id));
            }
        }

        public void SetAsDispatched(OutboxMessage outboxMessage)
        {
        }

        ConcurrentDictionary<string, OutboxMessage> storage = new ConcurrentDictionary<string, OutboxMessage>();
    }
}