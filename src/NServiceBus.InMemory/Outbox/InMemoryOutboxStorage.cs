#nullable enable

namespace NServiceBus.Persistence.InMemory;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Extensibility;
using Outbox;

class InMemoryOutboxStorage(InMemoryStorage storage) : IOutboxStorage
{
    public InMemoryOutboxStorage() : this(new InMemoryStorage())
    {
    }

    public Task<OutboxMessage> Get(string messageId, ContextBag context, CancellationToken cancellationToken = default)
    {
        if (!Storage.TryGetValue(messageId, out var storedMessage))
        {
            return NoOutboxMessageTask!;
        }

        return Task.FromResult(new OutboxMessage(messageId, storedMessage.TransportOperations));
    }

    public Task<IOutboxTransaction> BeginTransaction(ContextBag context, CancellationToken cancellationToken = default)
        => Task.FromResult<IOutboxTransaction>(new InMemoryOutboxTransaction());

    public Task Store(OutboxMessage message, IOutboxTransaction transaction, ContextBag context, CancellationToken cancellationToken = default)
    {
        var tx = (InMemoryOutboxTransaction)transaction;
        tx.Enlist(
            new StoreOperationState(Storage, message.MessageId, message.TransportOperations.Select(CopyOperation).ToArray()),
            static state =>
            {
                if (!state.Storage.TryAdd(state.MessageId, new StoredOutboxMessage(state.MessageId, state.TransportOperations)))
                {
                    throw new Exception($"Outbox message with id '{state.MessageId}' is already present in storage.");
                }
            },
            static state => state.Storage.TryRemove(state.MessageId, out _));

        return Task.CompletedTask;
    }

    public Task SetAsDispatched(string messageId, ContextBag context, CancellationToken cancellationToken = default)
    {
        if (!Storage.TryGetValue(messageId, out var storedMessage))
        {
            return Task.CompletedTask;
        }

        storedMessage.MarkAsDispatched(DateTime.UtcNow);
        return Task.CompletedTask;
    }

    public void RemoveEntriesOlderThan(DateTime dateTime)
    {
        foreach (var entry in Storage)
        {
            var storedMessage = entry.Value;
            if (storedMessage.Dispatched && storedMessage.DispatchedAt < dateTime)
            {
                Storage.TryRemove(entry.Key, out _);
            }
        }
    }

    internal ConcurrentDictionary<string, StoredOutboxMessage> Messages => Storage;

    readonly record struct StoreOperationState(
        ConcurrentDictionary<string, StoredOutboxMessage> Storage,
        string MessageId,
        TransportOperation[] TransportOperations);

    static TransportOperation CopyOperation(TransportOperation operation)
    {
        var headers = operation.Headers != null
            ? new Dictionary<string, string>(operation.Headers)
            : [];

        var options = operation.Options != null
            ? new Transport.DispatchProperties(operation.Options)
            : [];

        var body = operation.Body.IsEmpty
            ? []
            : operation.Body.ToArray();

        return new TransportOperation(operation.MessageId, options, body, headers);
    }

    ConcurrentDictionary<string, StoredOutboxMessage> Storage { get; } = storage.OutboxMessages;
    static readonly Task<OutboxMessage?> NoOutboxMessageTask = Task.FromResult(default(OutboxMessage));
}
