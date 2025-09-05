namespace NServiceBus.Core.Tests.Fakes;

using System;
using System.Threading;
using System.Threading.Tasks;
using Outbox;

public sealed class FakeOutboxTransaction : IOutboxTransaction
{
    public FakeTransaction Transaction { get; private set; } = new();

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