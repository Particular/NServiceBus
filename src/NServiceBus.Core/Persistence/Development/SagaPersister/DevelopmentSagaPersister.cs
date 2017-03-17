namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Extensibility;
    using Persistence;
    using Sagas;

    class DevelopmentSagaPersister : ISagaPersister
    {
        public DevelopmentSagaPersister(Dictionary<Type, SagaManifest> sagaManifests)
        {
            this.sagaManifests = sagaManifests;
        }

        public Task Save(IContainSagaData sagaData, SagaCorrelationProperty correlationProperty, SynchronizedStorageSession session, ContextBag context)
        {
            var developmentSyncronizedStorageSession = (DevelopmentSyncronizedStorageSession) session;
            var manifest = sagaManifests[sagaData.GetType()];

            var sagaFile = developmentSyncronizedStorageSession.CreateNew(sagaData.Id, manifest);

            sagaFile.Write(sagaData);

            return TaskEx.CompletedTask;
        }

        public Task Update(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
        {
            var developmentSyncronizedStorageSession = (DevelopmentSyncronizedStorageSession) session;
            var sagaFile = developmentSyncronizedStorageSession.GetSagaFile(sagaData);

            sagaFile.Write(sagaData);

            return TaskEx.CompletedTask;
        }

        public Task<TSagaData> Get<TSagaData>(Guid sagaId, SynchronizedStorageSession session, ContextBag context) where TSagaData : IContainSagaData
        {
            return Get<TSagaData>(sagaId, session);
        }

        public Task<TSagaData> Get<TSagaData>(string propertyName, object propertyValue, SynchronizedStorageSession session, ContextBag context) where TSagaData : IContainSagaData
        {
            return Get<TSagaData>(DeterministicGuid.Create(propertyValue), session);
        }

        public Task Complete(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
        {
            var developmentSyncronizedStorageSession = (DevelopmentSyncronizedStorageSession) session;
            var sagaFile = developmentSyncronizedStorageSession.GetSagaFile(sagaData);

            sagaFile.Delete();

            return TaskEx.CompletedTask;
        }

        Task<TSagaData> Get<TSagaData>(Guid sagaId, SynchronizedStorageSession session) where TSagaData : IContainSagaData
        {
            var manifest = sagaManifests[typeof(TSagaData)];
            var developmentSyncronizedStorageSession = (DevelopmentSyncronizedStorageSession) session;

            SagaStorageFile sagaStorageFile;

            if (!developmentSyncronizedStorageSession.TryOpenAndLockSaga(sagaId, manifest, out sagaStorageFile))
            {
                return Task.FromResult(default(TSagaData));
            }

            return Task.FromResult((TSagaData) sagaStorageFile.Read());
        }

        readonly Dictionary<Type, SagaManifest> sagaManifests;
    }
}