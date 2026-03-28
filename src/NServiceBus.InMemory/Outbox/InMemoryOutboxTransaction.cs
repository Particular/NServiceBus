#nullable enable

namespace NServiceBus.Persistence.InMemory;

using System;
using System.Threading;
using System.Threading.Tasks;
using Outbox;

class InMemoryOutboxTransaction : IOutboxTransaction
{
    public InMemoryStorageTransaction? Transaction { get; private set; } = new();

    public void Enlist<TState>(TState state, Action<TState> apply, Action<TState>? rollback = null)
    {
        ArgumentNullException.ThrowIfNull(apply);
        ArgumentNullException.ThrowIfNull(Transaction);

        Transaction.Enlist(state, apply, rollback);
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
