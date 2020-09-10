namespace NServiceBus
{
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Persistence;

    class InMemorySynchronizedStorage : ISynchronizedStorage
    {
        public Task<CompletableSynchronizedStorageSession> OpenSession(ContextBag contextBag, CancellationToken cancellationToken)
        {
            var session = (CompletableSynchronizedStorageSession) new InMemorySynchronizedStorageSession();
            return Task.FromResult(session);
        }
    }
}