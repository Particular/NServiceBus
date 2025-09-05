namespace NServiceBus.AcceptanceTesting;

using System;
using System.Threading;
using System.Threading.Tasks;
using Outbox;

class AcceptanceTestingOutboxTransaction : IOutboxTransaction
{
    public AcceptanceTestingTransaction Transaction { get; private set; } = new();

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

    public Task Commit(CancellationToken cancellationToken = default)
    {
        Transaction.Commit();
        return Task.CompletedTask;
    }

    public void Enlist(Action action) => Transaction.Enlist(action);
}