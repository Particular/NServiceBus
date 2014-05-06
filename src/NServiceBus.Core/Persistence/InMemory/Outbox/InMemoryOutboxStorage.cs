namespace NServiceBus.Persistence.InMemory.Outbox
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Outbox;

    public class InMemoryOutboxStorage : IOutboxStorage
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

        public IDisposable OpenSession()
        {
            return new EmptyDisposable();
        }

        public void StoreAndCommit(string messageId, IEnumerable<TransportOperation> transportOperations)
        {
            if (!storage.TryAdd(messageId, new StoredMessage(messageId, transportOperations)))
            {
                throw new ConcurrencyException(string.Format("Outbox message with id '{0}' is already present in storage.", messageId));
            }
        }

        public void SetAsDispatched(string messageId)
        {
            var expectedState = new StoredMessage(messageId, Enumerable.Empty<TransportOperation>());
            StoredMessage storedMessage;

            storage.TryGetValue(messageId, out storedMessage);

            if (!storage.TryUpdate(messageId, new StoredMessage(messageId, storedMessage.TransportOperations)
            {
                Dispatched = true
            }, expectedState))
            {
                throw new ConcurrencyException(string.Format("Outbox message with id '{0}' is has already been updated by another thread.", messageId));
            }
        }

        ConcurrentDictionary<string, StoredMessage> storage = new ConcurrentDictionary<string, StoredMessage>();

        class EmptyDisposable : IDisposable
        {
            public void Dispose()
            {
            }
        }

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