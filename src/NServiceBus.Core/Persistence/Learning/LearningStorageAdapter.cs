namespace NServiceBus
{
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Outbox;
    using Persistence;
    using Transport;

    class LearningStorageAdapter : ISynchronizedStorageAdapter
    {
        public Task<ICompletableSynchronizedStorageSession> TryAdapt(IOutboxTransaction transaction, ContextBag context, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<ICompletableSynchronizedStorageSession>(null);
        }

        public Task<ICompletableSynchronizedStorageSession> TryAdapt(TransportTransaction transportTransaction, ContextBag context, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<ICompletableSynchronizedStorageSession>(null);
        }
    }
}