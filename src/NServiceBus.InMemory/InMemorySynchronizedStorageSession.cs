namespace NServiceBus.Persistence.InMemory;

using System;
using System.Threading;
using System.Threading.Tasks;
using Extensibility;
using Outbox;
using Persistence;
using Transport;

class InMemorySynchronizedStorageSession : ICompletableSynchronizedStorageSession
{
    public InMemoryStorageTransaction? Transaction { get; private set; }

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

    public ValueTask<bool> TryOpen(IOutboxTransaction transaction, ContextBag context,
        CancellationToken cancellationToken = default)
    {
        if (transaction is InMemoryOutboxTransaction inMemoryOutboxTransaction)
        {
            Transaction = inMemoryOutboxTransaction.Transaction;
            ownsTransaction = false;
            return new ValueTask<bool>(true);
        }

        return new ValueTask<bool>(false);
    }

    public ValueTask<bool> TryOpen(TransportTransaction transportTransaction, ContextBag context,
        CancellationToken cancellationToken = default)
    {
        // For ReceiveOnly with transport seam - stub for now
        return new ValueTask<bool>(false);
    }

    public Task Open(ContextBag context, CancellationToken cancellationToken = default)
    {
        ownsTransaction = true;
        Transaction = new InMemoryStorageTransaction();
        return Task.CompletedTask;
    }

    public Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        if (ownsTransaction && Transaction is not null)
        {
            Transaction.Commit();
        }

        return Task.CompletedTask;
    }

    public void Enlist<TState>(TState state, Action<TState> apply, Action<TState>? rollback = null)
    {
        ArgumentNullException.ThrowIfNull(apply);
        ArgumentNullException.ThrowIfNull(Transaction);
        Transaction.Enlist(state, apply, rollback);
    }

    bool ownsTransaction;
}
