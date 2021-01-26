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
        public Task<CompletableSynchronizedStorageSession> TryAdapt(OutboxTransaction transaction, ContextBag context, CancellationToken cancellationToken)
        {
            return EmptyResult;
        }

        public Task<CompletableSynchronizedStorageSession> TryAdapt(TransportTransaction transportTransaction, ContextBag context, CancellationToken cancellationToken)
        {
            return EmptyResult;
        }

        internal static readonly Task<CompletableSynchronizedStorageSession> EmptyResult = Task.FromResult<CompletableSynchronizedStorageSession>(new NoOpCompletableSynchronizedStorageSession());
    }
}