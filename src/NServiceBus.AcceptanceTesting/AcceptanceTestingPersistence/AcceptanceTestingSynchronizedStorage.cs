namespace NServiceBus.AcceptanceTesting
{
    using System.Threading.Tasks;
    using Extensibility;
    using Persistence;

    class AcceptanceTestingSynchronizedStorage : ISynchronizedStorage
    {
        public Task<CompletableSynchronizedStorageSession> OpenSession(ContextBag contextBag)
        {
            var session = (CompletableSynchronizedStorageSession)new AcceptanceTestingSynchronizedStorageSession();
            return Task.FromResult(session);
        }
    }
}