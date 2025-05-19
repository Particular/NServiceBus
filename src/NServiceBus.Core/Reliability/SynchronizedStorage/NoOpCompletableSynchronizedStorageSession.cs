namespace NServiceBus;

using System.Threading;
using System.Threading.Tasks;
using Extensibility;
using Outbox;
using Persistence;
using Transport;

sealed class NoOpCompletableSynchronizedStorageSession : ICompletableSynchronizedStorageSession
{
    public ValueTask<bool> TryOpen(IOutboxTransaction transaction, ContextBag context,
        CancellationToken cancellationToken = default) =>
        new ValueTask<bool>(true);

    public ValueTask<bool> TryOpen(TransportTransaction transportTransaction, ContextBag context,
        CancellationToken cancellationToken = default) =>
        new ValueTask<bool>(false);

    public Task Open(ContextBag context, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task CompleteAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public void Dispose()
    {
    }

    public ValueTask DisposeAsync() => default;

    public static readonly ICompletableSynchronizedStorageSession Instance = new NoOpCompletableSynchronizedStorageSession();
}