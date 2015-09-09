namespace NServiceBus.InMemory.Outbox
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Outbox;

    class InMemoryOutboxStorage : IOutboxStorage
    {
        public Task<OutboxMessage> Get(string messageId, OutboxStorageOptions options)
        {
            StoredMessage storedMessage;
            if (!storage.TryGetValue(messageId, out storedMessage))
            {
                return Task.FromResult(default(OutboxMessage));
            }

            var message = new OutboxMessage(messageId);
            message.TransportOperations.AddRange(storedMessage.TransportOperations);
            return Task.FromResult(message);
        }

        public Task Store(OutboxMessage message, OutboxStorageOptions options)
        {
            if (!storage.TryAdd(message.MessageId, new StoredMessage(message.MessageId, message.TransportOperations)))
            {
                throw new Exception(string.Format("Outbox message with id '{0}' is already present in storage.", message.MessageId));
            }
            return Task.FromResult(0);
        }

        public Task SetAsDispatched(string messageId, OutboxStorageOptions options)
        {
            StoredMessage storedMessage;

            if (!storage.TryGetValue(messageId, out storedMessage))
            {
                return Task.FromResult(0);
            }

            storedMessage.TransportOperations.Clear();
            storedMessage.Dispatched = true;

            return Task.FromResult(0);
        }

        ConcurrentDictionary<string, StoredMessage> storage = new ConcurrentDictionary<string, StoredMessage>();

        class StoredMessage
        {
            public StoredMessage(string messageId, IList<TransportOperation> transportOperations)
            {
                TransportOperations = transportOperations;
                Id = messageId;
                StoredAt = DateTime.UtcNow;
            }

            public string Id { get; private set; }

            public bool Dispatched { get; set; }

            public DateTime StoredAt { get; set; }

            public IList<TransportOperation> TransportOperations { get; private set; }

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
                return Equals((StoredMessage)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((Id != null ? Id.GetHashCode() : 0) * 397) ^ Dispatched.GetHashCode();
                }
            }
        }

        public void RemoveEntriesOlderThan(DateTime dateTime)
        {
            var entriesToRemove = storage
                .Where(e => e.Value.Dispatched && e.Value.StoredAt < dateTime)
                .Select(e => e.Key)
                .ToList();

            foreach (var entry in entriesToRemove)
            {
                StoredMessage toRemove;

                storage.TryRemove(entry, out toRemove);
            }
        }
    }
}