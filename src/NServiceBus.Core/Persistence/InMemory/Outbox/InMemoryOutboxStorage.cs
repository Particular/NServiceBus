namespace NServiceBus.InMemory.Outbox
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using NServiceBus.Outbox;
    using Persistence;

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
            if (!storage.TryAdd(messageId, new StoredMessage(messageId, transportOperations)))
            {
                throw new ConcurrencyException(string.Format("Outbox message with id '{0}' is already present in storage.", messageId));
            }
        }

        public void SetAsDispatched(string messageId)
        {
            //no op since this is only relevant for cleanups
        }

        ConcurrentDictionary<string, StoredMessage> storage = new ConcurrentDictionary<string, StoredMessage>();

        class StoredMessage
        {
            public StoredMessage(string messageId, IEnumerable<TransportOperation> transportOperations)
            {
                this.transportOperations = transportOperations;
                Id = messageId;
            }

            public string Id { get; private set; }

            public bool Dispatched { get; set; }

            public IEnumerable<TransportOperation> TransportOperations
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
                return Equals((StoredMessage) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((Id != null ? Id.GetHashCode() : 0)*397) ^ Dispatched.GetHashCode();
                }
            }

            readonly IEnumerable<TransportOperation> transportOperations;
        }
    }
}