namespace NServiceBus
{
    using System.Threading.Tasks;
    using Extensibility;
    using Janitor;
    using Outbox;
    using Persistence;
    using Transport;

    [SkipWeaving]
    sealed class CompletableSynchronizedStorageSessionAdapter : CompletableSynchronizedStorageSession
    {
        public CompletableSynchronizedStorageSessionAdapter(ISynchronizedStorageAdapter synchronizedStorageAdapter, ISynchronizedStorage synchronizedStorage)
        {
            this.synchronizedStorage = synchronizedStorage;
            this.synchronizedStorageAdapter = synchronizedStorageAdapter;
        }
        public CompletableSynchronizedStorageSession AdaptedSession { get; private set; }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            AdaptedSession?.Dispose();
            disposed = true;
        }

        public Task CompleteAsync() => AdaptedSession.CompleteAsync();

        public async Task<bool> TryOpen(OutboxTransaction transaction, ContextBag context)
        {
            AdaptedSession = await synchronizedStorageAdapter.TryAdapt(transaction, context).ConfigureAwait(false);

            return AdaptedSession != null;
        }

        public async Task<bool> TryOpen(TransportTransaction transportTransaction, ContextBag context)
        {
            AdaptedSession = await synchronizedStorageAdapter.TryAdapt(transportTransaction, context).ConfigureAwait(false);

            return AdaptedSession != null;
        }

        public async Task Open(ContextBag contextBag) => AdaptedSession = await synchronizedStorage.OpenSession(contextBag).ConfigureAwait(false);

        bool disposed;

        readonly ISynchronizedStorageAdapter synchronizedStorageAdapter;
        readonly ISynchronizedStorage synchronizedStorage;
    }
}