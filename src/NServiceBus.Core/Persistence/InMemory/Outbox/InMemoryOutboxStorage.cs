namespace NServiceBus.Persistence.InMemory.Outbox
{
    using System.Collections.Concurrent;
    using NServiceBus.Outbox;

    class InMemoryOutboxStorage:IOutboxStorage
    {
        public OutboxMessage Get(string messageId)
        {
            StoredMessage storedMessage;

            if(!storage.TryGetValue(messageId, out storedMessage))
            {
                return null;
            }
            
            return storedMessage.OutboxMessage;
        }

        public void Store(OutboxMessage outboxMessage)
        {
            if (!storage.TryAdd(outboxMessage.Id, new StoredMessage(outboxMessage)))
            {
                throw new ConcurrencyException(string.Format("Outbox message with id: '{0}' is already present in storage",outboxMessage.Id));
            }
        }

        public void SetAsDispatched(OutboxMessage outboxMessage)
        {
            var expectedState = new StoredMessage(outboxMessage.Id);

            if (!storage.TryUpdate(outboxMessage.Id, new StoredMessage(outboxMessage), expectedState))
            {
                throw new ConcurrencyException(string.Format("Outbox message with id: '{0}' is has been updated by another thread", outboxMessage.Id));
            }
        }

        ConcurrentDictionary<string, StoredMessage> storage = new ConcurrentDictionary<string, StoredMessage>();

        class StoredMessage
        {
            public StoredMessage(OutboxMessage outboxMessage)
            {
                OutboxMessage = outboxMessage;
                Id = outboxMessage.Id;
                Dispatched = outboxMessage.Dispatched;
            }

            public StoredMessage(string outboxMessageId)
            {
                Id = outboxMessageId;
            }

            protected bool Equals(StoredMessage other)
            {
                return string.Equals(Id, other.Id) && Dispatched.Equals(other.Dispatched);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj))
                {
                    return false;
                }
                if (ReferenceEquals(this, obj))
                {
                    return true;
                }
                if (obj.GetType() != GetType())
                {
                    return false;
                }
                return Equals((StoredMessage) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((Id != null ? Id.GetHashCode() : 0)*397) ^ Dispatched.GetHashCode();
                }
            }

            public string Id{ get; private set; }

            public bool Dispatched { get; private set; }

            public OutboxMessage OutboxMessage { get; private set; }
        }
    }
}