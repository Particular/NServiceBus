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

class InMemoryOutboxStorage : IOutboxStorage
{
    public Task<OutboxMessage> Get(string messageId, ContextBag context, CancellationToken cancellationToken = default)
    {
        if (!storage.TryGetValue(messageId, out var storedMessage))
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
        tx.Enlist(() =>
        {
            if (!storage.TryAdd(message.MessageId, new StoredOutboxMessage(message.MessageId, message.TransportOperations.Select(CopyOperation).ToArray())))
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

    readonly ConcurrentDictionary<string, StoredOutboxMessage> storage = new();
    static readonly Task<OutboxMessage?> NoOutboxMessageTask = Task.FromResult(default(OutboxMessage));
}
