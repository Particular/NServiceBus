namespace NServiceBus.Persistence
{
    using System.Threading.Tasks;
    using Extensibility;
    using Janitor;
    using Outbox;
    using Transport;

    [SkipWeaving]
    class CompletableSynchronizedStorageSessionAdapter : ICompletableSynchronizedStorageSession
    {
        readonly ISynchronizedStorageAdapter adapter;
        readonly ISynchronizedStorage syncStorage;

        CompletableSynchronizedStorageSession session;

        public CompletableSynchronizedStorageSessionAdapter(ISynchronizedStorageAdapter adapter, ISynchronizedStorage syncStorage)
        {
            this.adapter = adapter;
            this.syncStorage = syncStorage;
        }

        public void Dispose() => session.Dispose();

        public async Task<bool> TryOpen(OutboxTransaction transaction, ContextBag context)
        {
            session = await adapter.TryAdapt(transaction, context).ConfigureAwait(false);

            return session != null;
        }

        public async Task<bool> TryOpen(TransportTransaction transportTransaction, ContextBag context)
        {
            session = await adapter.TryAdapt(transportTransaction, context).ConfigureAwait(false);

            return session != null;
        }

        public async Task Open(ContextBag contextBag)
        {
            session = await syncStorage.OpenSession(contextBag).ConfigureAwait(false);
        }

        public Task CompleteAsync() => session.CompleteAsync();
    }
}