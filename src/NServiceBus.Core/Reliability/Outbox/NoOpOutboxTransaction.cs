#nullable enable

namespace NServiceBus;

using System.Threading;
using System.Threading.Tasks;
using Outbox;

sealed class NoOpOutboxTransaction : IOutboxTransaction
{
    // Stateless, so a single instance can be shared by every message.
    public static readonly NoOpOutboxTransaction Instance = new();

    public void Dispose()
    {
    }

    public ValueTask DisposeAsync() => default;

    public Task Commit(CancellationToken cancellationToken = default) => Task.CompletedTask;
}