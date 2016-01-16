namespace NServiceBus
{
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Persistence;

    class InMemorySynchronizedStorage : ISynchronizedStorage
    {
        public Task<CompletableSynchronizedStorageSession> OpenSession(ContextBag contextBag)
        {
            return InMemorySynchronizedSessionTask;
        }

        static Task<CompletableSynchronizedStorageSession> InMemorySynchronizedSessionTask = Task.FromResult((CompletableSynchronizedStorageSession)new InMemorySynchronizedStorageSession());
    }
}