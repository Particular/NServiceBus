﻿namespace NServiceBus.InMemory.Outbox
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Web.WebSockets;
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

            return Task.FromResult(new OutboxMessage(messageId, storedMessage.TransportOperations));
        }

        public Task Store(OutboxMessage message, OutboxStorageOptions options)
        {
            if (!storage.TryAdd(message.MessageId, new StoredMessage(message.MessageId, message.TransportOperations.ToList())))
            {
                throw new Exception($"Outbox message with id '{message.MessageId}' is already present in storage.");
            }
            return TaskEx.Completed;
        }

        public Task SetAsDispatched(string messageId, OutboxStorageOptions options)
        {
            StoredMessage storedMessage;

            if (!storage.TryGetValue(messageId, out storedMessage))
            {
                return TaskEx.Completed;
            }

            storedMessage.TransportOperations.Clear();
            storedMessage.Dispatched = true;

            return TaskEx.Completed;
        }

        ConcurrentDictionary<string, StoredMessage> storage = new ConcurrentDictionary<string, StoredMessage>();

        class StoredMessage
        {
            public StoredMessage(string messageId, IList<TransportOperation> transportOperations)
            {
                TransportOperations = transportOperations;
                Id = messageId;
                StoredAt = DateTime.UtcNow;

                foreach (var operation in TransportOperations)
                {
                    var body = operation.Body;
                    var ms = new MemoryStream();
                    body.CopyTo(ms);
                    operation.Body = ms;
                }
            }

            public string Id { get; }

            public bool Dispatched { get; set; }

            public DateTime StoredAt { get; }

            public IList<TransportOperation> TransportOperations { get; }

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
                    return ((Id?.GetHashCode() ?? 0) * 397) ^ Dispatched.GetHashCode();
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