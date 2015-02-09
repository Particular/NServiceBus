namespace NServiceBus.InMemory.Outbox
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Outbox;

    class InMemoryOutboxStorage : IOutboxStorage
    {
        public bool TryGet(string messageId, out OutboxMessage message)
        {
            StoredMessage storedMessage;
            message = null;

            if (!storage.TryGetValue(messageId, out storedMessage))
            {
                return false;
            }

            message = new OutboxMessage(messageId);
            message.TransportOperations.AddRange(storedMessage.TransportOperations);

            return true;
        }

        public void Store(string messageId, IEnumerable<TransportOperation> transportOperations)
        {
            if (!storage.TryAdd(messageId, new StoredMessage(messageId, transportOperations.ToList())))
            {
                throw new Exception(string.Format("Outbox message with id '{0}' is already present in storage.", messageId));
            }
        }

        public void SetAsDispatched(string messageId)
        {
            StoredMessage storedMessage;

            if (!storage.TryGetValue(messageId, out storedMessage))
            {
                return;
            }

            storedMessage.TransportOperations.Clear();
            storedMessage.Dispatched = true;
        }

        ConcurrentDictionary<string, StoredMessage> storage = new ConcurrentDictionary<string, StoredMessage>();

        class StoredMessage
        {
            public StoredMessage(string messageId, IList<TransportOperation> transportOperations)
            {
                this.transportOperations = transportOperations;
                Id = messageId;
                StoredAt = DateTime.UtcNow;
            }

            public string Id { get; private set; }

            public bool Dispatched { get; set; }

            public DateTime StoredAt { get; set; }

            public IList<TransportOperation> TransportOperations
            {
                get { return transportOperations; }
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
                return Equals((StoredMessage)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((Id != null ? Id.GetHashCode() : 0) * 397) ^ Dispatched.GetHashCode();
                }
            }

            readonly IList<TransportOperation> transportOperations;
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