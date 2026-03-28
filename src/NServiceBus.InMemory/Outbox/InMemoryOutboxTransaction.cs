#nullable enable

namespace NServiceBus.Persistence.InMemory;

using System;
using System.Threading;
using System.Threading.Tasks;
using Outbox;

class InMemoryOutboxTransaction : IOutboxTransaction
{
    public InMemoryStorageTransaction? Transaction { get; private set; } = new();

    public void Enlist(Func<Action?> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(Transaction);

        Transaction.Enlist(operation);
    }

    public Task Commit(CancellationToken cancellationToken = default)
    {
        Transaction?.Commit();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (Transaction is null)
        {
            return;
        }

        Transaction = null;
    }

    public ValueTask DisposeAsync()
    {
        if (Transaction is null)
        {
            return default;
        }

        Transaction = null;

        return default;
    }
}
