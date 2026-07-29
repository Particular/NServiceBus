namespace NServiceBus.AcceptanceTesting;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

        // Return copies so callers (the dispatch pipeline) own their header dictionaries. The dispatch
        // pipeline pools and clears outgoing header dictionaries after dispatch, so the outbox must hand out
        // fresh dictionaries on every Get. Sharing the stored references would let dispatch clear/pool the
        // stored dictionaries, corrupting storage and leaking aliased dictionaries into the header pool.
        return Task.FromResult(new OutboxMessage(messageId, storedMessage.TransportOperations.Select(CopyOperation).ToArray()));
    }

    public Task<IOutboxTransaction> BeginTransaction(ContextBag context, CancellationToken cancellationToken = default) => Task.FromResult<IOutboxTransaction>(new AcceptanceTestingOutboxTransaction());

    public Task Store(OutboxMessage message, IOutboxTransaction transaction, ContextBag context, CancellationToken cancellationToken = default)
    {
        var tx = (AcceptanceTestingOutboxTransaction)transaction;
        tx.Enlist(() =>
        {
            if (!storage.TryAdd(message.MessageId, new StoredMessage(message.MessageId, message.TransportOperations.Select(CopyOperation).ToArray())))
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

    // Copies headers/options/body into fresh instances so stored data is independent of the live pipeline
    // dictionaries (which are pooled and cleared after dispatch). Mirrors NonDurableOutboxStorage.CopyOperation.
    static TransportOperation CopyOperation(TransportOperation operation)
    {
        var headers = operation.Headers != null
            ? new Dictionary<string, string>(operation.Headers)
            : [];

        var options = operation.Options != null
            ? new NServiceBus.Transport.DispatchProperties(operation.Options)
            : [];

        var body = operation.Body.IsEmpty
            ? []
            : operation.Body.ToArray();

        return new TransportOperation(operation.MessageId, options, body, headers);
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