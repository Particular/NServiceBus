namespace NServiceBus.Persistence
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using Janitor;
    using Outbox;
    using Transport;

    [SkipWeaving]
    class CompletableSynchronizedStorageSessionAdapter : IDisposable
    {
        readonly ISynchronizedStorageAdapter adapter;
        readonly ISynchronizedStorage syncStorage;

        public CompletableSynchronizedStorageSessionAdapter(ISynchronizedStorageAdapter adapter, ISynchronizedStorage syncStorage)
        {
            this.adapter = adapter;
            this.syncStorage = syncStorage;
        }

        public void Dispose() => Session?.Dispose();

        public CompletableSynchronizedStorageSession Session { get; private set; }

        public async Task<bool> TryOpen(OutboxTransaction transaction, ContextBag context)
        {
            Session = await adapter.TryAdapt(transaction, context).ConfigureAwait(false);

            return Session != null;
        }

        public async Task<bool> TryOpen(TransportTransaction transportTransaction, ContextBag context)
        {
            Session = await adapter.TryAdapt(transportTransaction, context).ConfigureAwait(false);

            return Session != null;
        }

        public async Task Open(ContextBag contextBag)
        {
            Session = await syncStorage.OpenSession(contextBag).ConfigureAwait(false);
        }

        public Task CompleteAsync() => Session.CompleteAsync();
    }
}