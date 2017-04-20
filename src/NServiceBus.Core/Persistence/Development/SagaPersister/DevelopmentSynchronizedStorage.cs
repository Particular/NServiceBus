namespace NServiceBus
{
    using System.Threading.Tasks;
    using Extensibility;
    using Persistence;

    class DevelopmentSynchronizedStorage : ISynchronizedStorage
    {
        public DevelopmentSynchronizedStorage(SagaManifestCollection sagaManifests)
        {
            this.sagaManifests = sagaManifests;
        }

        public Task<CompletableSynchronizedStorageSession> OpenSession(ContextBag contextBag)
        {
            return Task.FromResult<CompletableSynchronizedStorageSession>(new DevelopmentSynchronizedStorageSession(sagaManifests));
        }

        SagaManifestCollection sagaManifests;
    }
}