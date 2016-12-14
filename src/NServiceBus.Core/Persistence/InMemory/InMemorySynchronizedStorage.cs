namespace NServiceBus
{
    using System.Threading.Tasks;
    using Extensibility;
    using Persistence;

    class InMemorySynchronizedStorage : ISynchronizedStorage
    {
        InMemorySagaPersister inMemorySagaPersister;

        public InMemorySynchronizedStorage() : this(null)
        {
        }

        public InMemorySynchronizedStorage(InMemorySagaPersister sagaPersister)
        {
            inMemorySagaPersister = sagaPersister;
        }

        public Task<CompletableSynchronizedStorageSession> OpenSession(ContextBag contextBag)
        {
            var session = (CompletableSynchronizedStorageSession) new InMemorySynchronizedStorageSession(inMemorySagaPersister);
            return Task.FromResult(session);
        }
    }
}