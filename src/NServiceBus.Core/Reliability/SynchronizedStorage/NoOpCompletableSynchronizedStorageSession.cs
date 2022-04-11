namespace NServiceBus
{
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Janitor;
    using Outbox;
    using Persistence;
    using Transport;

    // Do not allow Fody to weave the IDisposable for us so that other threads can still access the instance of this class
    // even after it has been disposed.
    [SkipWeaving]
    sealed class NoOpCompletableSynchronizedStorageSession : ICompletableSynchronizedStorageSession
    {
        public ValueTask<bool> OpenSession(IOutboxTransaction transaction, ContextBag context,
            CancellationToken cancellationToken = default) =>
            new ValueTask<bool>(true);

        public ValueTask<bool> OpenSession(TransportTransaction transportTransaction, ContextBag context,
            CancellationToken cancellationToken = default) =>
            new ValueTask<bool>(false);

        public ValueTask OpenSession(ContextBag contextBag, CancellationToken cancellationToken = default) =>
            new ValueTask();

        public Task CompleteAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public void Dispose()
        {
        }

        public static readonly ICompletableSynchronizedStorageSession Instance = new NoOpCompletableSynchronizedStorageSession();
    }
}