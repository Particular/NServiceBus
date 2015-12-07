namespace NServiceBus
{
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Persistence;

    class InMemorySynchronizedStorage : ISynchronizedStorage
    {
        public Task<ICompletableSynchronizedStorageSession> OpenSession(ContextBag contextBag)
        {
            var session = (ICompletableSynchronizedStorageSession)new InMemorySynchronizedStorageSession();
            return Task.FromResult(session);
        }
    }
}