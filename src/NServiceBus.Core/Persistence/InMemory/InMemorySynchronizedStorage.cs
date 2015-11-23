namespace NServiceBus
{
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Persistence;

    class InMemorySynchronizedStorage : ISynchronizedStorage
    {
        public Task<CompletableSynchronizedStorageSession> OpenSession(ContextBag contextBag)
        {
            var session = (CompletableSynchronizedStorageSession)new InMemorySynchronizedStorageSession();
            return Task.FromResult(session);
        }
    }
}