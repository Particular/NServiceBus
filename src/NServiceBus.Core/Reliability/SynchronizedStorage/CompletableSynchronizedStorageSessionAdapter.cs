namespace NServiceBus.Persistence
{
    using System.Threading.Tasks;
    using Extensibility;
    using Janitor;
    using Outbox;
    using Transport;

    [SkipWeaving]
    sealed class CompletableSynchronizedStorageSessionAdapter : ICompletableSynchronizedStorageSession
    {
        public CompletableSynchronizedStorageSessionAdapter(ISynchronizedStorageAdapter synchronizedStorageAdapter, ISynchronizedStorage synchronizedStorage)
        {
            this.synchronizedStorage = synchronizedStorage;
            this.synchronizedStorageAdapter = synchronizedStorageAdapter;
        }
        public void Dispose() => session?.Dispose();

        public Task CompleteAsync() => throw new System.NotImplementedException();

        public async Task<bool> TryOpen(OutboxTransaction transaction, ContextBag context)
        {
            session = await synchronizedStorageAdapter.TryAdapt(transaction, context).ConfigureAwait(false);

            return session != null;
        }

        public async Task<bool> TryOpen(TransportTransaction transportTransaction, ContextBag context)
        {
            session = await synchronizedStorageAdapter.TryAdapt(transportTransaction, context).ConfigureAwait(false);

            return session != null;
        }

        public async Task Open(ContextBag contextBag) => session = await synchronizedStorage.OpenSession(contextBag).ConfigureAwait(false);

        readonly ISynchronizedStorageAdapter synchronizedStorageAdapter;
        readonly ISynchronizedStorage synchronizedStorage;
        CompletableSynchronizedStorageSession session;
    }
}