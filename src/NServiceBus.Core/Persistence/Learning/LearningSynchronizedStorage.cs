namespace NServiceBus
{
    using System.Threading.Tasks;
    using Extensibility;
    using Persistence;

    class LearningSynchronizedStorage : ISynchronizedStorage
    {
        public LearningSynchronizedStorage(SagaManifestCollection sagaManifests)
        {
            this.sagaManifests = sagaManifests;
        }

        public Task<CompletableSynchronizedStorageSession> OpenSession(ContextBag contextBag)
        {
            return Task.FromResult<CompletableSynchronizedStorageSession>(new LearningSynchronizedStorageSession(sagaManifests));
        }

        SagaManifestCollection sagaManifests;
    }
}