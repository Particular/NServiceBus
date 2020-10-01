namespace NServiceBus.AcceptanceTesting.AcceptanceTestingPersistence
{
    using System.Threading.Tasks;
    using Extensibility;
    using Persistence;

    public class AcceptanceTestingSynchronizedStorage : ISynchronizedStorage
    {
        public Task<CompletableSynchronizedStorageSession> OpenSession(ContextBag contextBag)
        {
            var session = (CompletableSynchronizedStorageSession) new AcceptanceTestingSynchronizedStorageSession();
            return Task.FromResult(session);
        }
    }
}