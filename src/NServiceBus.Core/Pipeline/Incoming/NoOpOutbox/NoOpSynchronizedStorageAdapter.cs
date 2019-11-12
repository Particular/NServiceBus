namespace NServiceBus.Features
{
    using System.Threading.Tasks;
    using Extensibility;
    using NServiceBus.Outbox;
    using Persistence;
    using Transport;

    class NoOpSynchronizedStorageAdapter : ISynchronizedStorageAdapter
    {
        public Task<CompletableSynchronizedStorageSession> TryAdapt(OutboxTransaction transaction, ContextBag context)
        {
            return EmptyResult;
        }

        public Task<CompletableSynchronizedStorageSession> TryAdapt(TransportTransaction transportTransaction, ContextBag context)
        {
            return EmptyResult;
        }

        internal static readonly Task<CompletableSynchronizedStorageSession> EmptyResult = Task.FromResult<CompletableSynchronizedStorageSession>(new NoOpCompletableSynchronizedStorageSession());
    }
}