namespace NServiceBus
{
    using System.Threading.Tasks;
    using Extensibility;
    using Outbox;
    using Persistence;
    using Transport;

    class DevelopmentStorageAdapter : ISynchronizedStorageAdapter
    {
        public Task<CompletableSynchronizedStorageSession> TryAdapt(OutboxTransaction transaction, ContextBag context)
        {
            return Task.FromResult<CompletableSynchronizedStorageSession>(null);
        }

        public Task<CompletableSynchronizedStorageSession> TryAdapt(TransportTransaction transportTransaction, ContextBag context)
        {
            return Task.FromResult<CompletableSynchronizedStorageSession>(null);
        }
    }
}