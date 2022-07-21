namespace NServiceBus
{
    using System.Threading.Tasks;
    using Extensibility;
    using Outbox;
    using Persistence;
    using Transport;

    class NoOpSynchronizedStorageAdapter : ISynchronizedStorageAdapter
    {
        public Task<CompletableSynchronizedStorageSession> TryAdapt(OutboxTransaction transaction, ContextBag context) => EmptyResult;

        public Task<CompletableSynchronizedStorageSession> TryAdapt(TransportTransaction transportTransaction, ContextBag context) => EmptyResult;

        internal static readonly Task<CompletableSynchronizedStorageSession> EmptyResult = Task.FromResult(NoOpCompletableSynchronizedStorageSession.Instance);
    }
}