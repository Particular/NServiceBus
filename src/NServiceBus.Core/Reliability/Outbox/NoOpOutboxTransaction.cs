namespace NServiceBus;

using System.Threading;
using System.Threading.Tasks;
using Outbox;

sealed class NoOpOutboxTransaction : IOutboxTransaction
{
    public void Dispose()
    {
    }

    public ValueTask DisposeAsync() => default;

    public Task Commit(CancellationToken cancellationToken = default) => Task.CompletedTask;
}