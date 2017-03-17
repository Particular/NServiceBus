namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using Persistence;
    using Sagas;

    class DevelopmentSagaPersister : ISagaPersister
    {
        public Task Save(IContainSagaData sagaData, SagaCorrelationProperty correlationProperty, SynchronizedStorageSession session, ContextBag context)
        {
            var developmentSyncronizedStorageSession = (DevelopmentSyncronizedStorageSession)session;

            var sagaFile = developmentSyncronizedStorageSession.CreateNew(sagaData.Id, sagaData.GetType());

            sagaFile.Write(sagaData);

            return TaskEx.CompletedTask;
        }

        public Task Update(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
        {
            var developmentSyncronizedStorageSession = (DevelopmentSyncronizedStorageSession)session;
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
            var developmentSyncronizedStorageSession = (DevelopmentSyncronizedStorageSession)session;
            var sagaFile = developmentSyncronizedStorageSession.GetSagaFile(sagaData);

            sagaFile.Delete();

            return TaskEx.CompletedTask;
        }

        Task<TSagaData> Get<TSagaData>(Guid sagaId, SynchronizedStorageSession session) where TSagaData : IContainSagaData
        {
            var developmentSyncronizedStorageSession = (DevelopmentSyncronizedStorageSession)session;

            SagaStorageFile sagaStorageFile;

            if (!developmentSyncronizedStorageSession.TryOpenAndLockSaga(sagaId, typeof(TSagaData), out sagaStorageFile))
            {
                return Task.FromResult(default(TSagaData));
            }

            return Task.FromResult((TSagaData)sagaStorageFile.Read());
        }
    }
}