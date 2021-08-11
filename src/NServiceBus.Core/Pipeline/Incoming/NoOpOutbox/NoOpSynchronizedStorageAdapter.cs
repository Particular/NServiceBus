namespace NServiceBus
{
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Outbox;
    using Persistence;
    using Transport;

    class NoOpSynchronizedStorageAdapter : ISynchronizedStorageAdapter
    {
        public Task<ICompletableSynchronizedStorageSession> TryAdapt(IOutboxTransaction transaction, ContextBag context, CancellationToken cancellationToken = default)
        {
            return EmptyResult;
        }

        public Task<ICompletableSynchronizedStorageSession> TryAdapt(TransportTransaction transportTransaction, ContextBag context, CancellationToken cancellationToken = default)
        {
            return EmptyResult;
        }

        internal static readonly Task<ICompletableSynchronizedStorageSession> EmptyResult = Task.FromResult<ICompletableSynchronizedStorageSession>(new NoOpCompletableSynchronizedStorageSession());
    }
}