namespace NServiceBus
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;
    using Extensibility;
    using Outbox;

    class InMemoryOutboxStorage : IOutboxStorage
    {
        public Task<OutboxMessage> Get(string messageId, ContextBag context)
        {
            StoredMessage storedMessage;
            if (!storage.TryGetValue(messageId, out storedMessage))
            {
                return NoOutboxMessageTask;
            }

            return Task.FromResult(new OutboxMessage(messageId, storedMessage.TransportOperations));
        }

        public Task<OutboxTransaction> BeginTransaction(ContextBag context)
        {
            return Task.FromResult<OutboxTransaction>(new InMemoryOutboxTransaction());
        }

        public Task Store(OutboxMessage message, OutboxTransaction transaction, ContextBag context)
        {
            var tx = (InMemoryOutboxTransaction) transaction;
            tx.Enlist(() =>
            {
                if (!storage.TryAdd(message.MessageId, new StoredMessage(message.MessageId, message.TransportOperations)))
                {
                    throw new Exception($"Outbox message with id '{message.MessageId}' is already present in storage.");
                }
            });
            return TaskEx.CompletedTask;
        }

        public Task SetAsDispatched(string messageId, ContextBag context)
        {
            StoredMessage storedMessage;

            if (!storage.TryGetValue(messageId, out storedMessage))
            {
                return TaskEx.CompletedTask;
            }

            storedMessage.MarkAsDispatched();
            return TaskEx.CompletedTask;
        }

        public void RemoveEntriesOlderThan(DateTime dateTime)
        {
            foreach (var entry in storage)
            {
                var storedMessage = entry.Value;
                if (storedMessage.Dispatched && storedMessage.StoredAt < dateTime)
                {
                    StoredMessage toRemove;

                    storage.TryRemove(entry.Key, out toRemove);
                }
            }
        }


        ConcurrentDictionary<string, StoredMessage> storage = new ConcurrentDictionary<string, StoredMessage>();
        static Task<OutboxMessage> NoOutboxMessageTask = Task.FromResult(default(OutboxMessage));

        class StoredMessage
        {
            public StoredMessage(string messageId, TransportOperation[] transportOperations)
            {
                TransportOperations = transportOperations;
                Id = messageId;
                StoredAt = DateTime.UtcNow;
            }

            public string Id { get; }

            public bool Dispatched { get; private set; }

            public DateTime StoredAt { get; }

            public TransportOperation[] TransportOperations { get; private set; }

            public void MarkAsDispatched()
            {
                Dispatched = true;
                TransportOperations = new TransportOperation[0];
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
                    return ((Id?.GetHashCode() ?? 0)*397) ^ Dispatched.GetHashCode();
                }
            }
        }
    }
}