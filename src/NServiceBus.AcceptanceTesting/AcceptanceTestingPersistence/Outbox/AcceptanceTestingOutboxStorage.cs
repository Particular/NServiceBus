namespace NServiceBus.AcceptanceTesting;

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Extensibility;
using NServiceBus.Outbox;

class AcceptanceTestingOutboxStorage : IOutboxStorage
{
    public Task<OutboxMessage> Get(string messageId, ContextBag context, CancellationToken cancellationToken = default)
    {
        if (!storage.TryGetValue(messageId, out var storedMessage))
        {
            return NoOutboxMessageTask!;
        }

        return Task.FromResult(new OutboxMessage(messageId, storedMessage.TransportOperations));
    }

    public Task<IOutboxTransaction> BeginTransaction(ContextBag context, CancellationToken cancellationToken = default) => Task.FromResult<IOutboxTransaction>(new AcceptanceTestingOutboxTransaction());

    public Task Store(OutboxMessage message, IOutboxTransaction transaction, ContextBag context, CancellationToken cancellationToken = default)
    {
        var tx = (AcceptanceTestingOutboxTransaction)transaction;
        tx.Enlist(() =>
        {
            if (!storage.TryAdd(message.MessageId, new StoredMessage(message.MessageId, message.TransportOperations.Select(o => o.DeepCopy()).ToArray())))
            {
                throw new Exception($"Outbox message with id '{message.MessageId}' is already present in storage.");
            }
        });
        return Task.CompletedTask;
    }

    public Task SetAsDispatched(string messageId, ContextBag context, CancellationToken cancellationToken = default)
    {
        if (!storage.TryGetValue(messageId, out var storedMessage))
        {
            return Task.CompletedTask;
        }

        storedMessage.MarkAsDispatched();
        return Task.CompletedTask;
    }

    public void RemoveEntriesOlderThan(DateTime dateTime)
    {
        foreach (var entry in storage)
        {
            var storedMessage = entry.Value;
            if (storedMessage.Dispatched && storedMessage.StoredAt < dateTime)
            {
                storage.TryRemove(entry.Key, out _);
            }
        }
    }

    readonly ConcurrentDictionary<string, StoredMessage> storage = new();
    static readonly Task<OutboxMessage?> NoOutboxMessageTask = Task.FromResult(default(OutboxMessage));

    class StoredMessage(string messageId, TransportOperation[] transportOperations)
    {
        public string Id { get; } = messageId;

        public bool Dispatched { get; private set; }

        public DateTime StoredAt { get; } = DateTime.UtcNow;

        public TransportOperation[] TransportOperations { get; private set; } = transportOperations;

        public void MarkAsDispatched()
        {
            Dispatched = true;
            TransportOperations = [];
        }

        protected bool Equals(StoredMessage other) => string.Equals(Id, other.Id) && Dispatched.Equals(other.Dispatched);

        public override bool Equals(object? obj)
        {
            if (obj is null)
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
}