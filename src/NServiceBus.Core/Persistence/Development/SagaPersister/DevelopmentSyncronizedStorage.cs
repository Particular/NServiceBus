namespace NServiceBus
{
    using System.Threading.Tasks;
    using Extensibility;
    using Persistence;

    class DevelopmentSyncronizedStorage : ISynchronizedStorage
    {
        public DevelopmentSyncronizedStorage(SagaManifestCollection sagaManifests)
        {
            this.sagaManifests = sagaManifests;
        }

        public Task<CompletableSynchronizedStorageSession> OpenSession(ContextBag contextBag)
        {
            return Task.FromResult<CompletableSynchronizedStorageSession>(new DevelopmentSyncronizedStorageSession(sagaManifests));
        }

        SagaManifestCollection sagaManifests;
    }
}