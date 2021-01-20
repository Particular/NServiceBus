namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using Persistence;
    using Sagas;

    class LearningSagaPersister : ISagaPersister
    {
        public Task Save(IContainSagaData sagaData, SagaCorrelationProperty correlationProperty, SynchronizedStorageSession session, ContextBag context)
        {
            var storageSession = (LearningSynchronizedStorageSession)session;
            return storageSession.Save(sagaData);
        }

        public Task Update(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
        {
            var storageSession = (LearningSynchronizedStorageSession)session;
            return storageSession.Update(sagaData);
        }

        public Task<TSagaData> Get<TSagaData>(Guid sagaId, SynchronizedStorageSession session, ContextBag context)
            where TSagaData : class, IContainSagaData
        {
            return Get<TSagaData>(sagaId, session);
        }

        public Task<TSagaData> Get<TSagaData>(string propertyName, object propertyValue, SynchronizedStorageSession session, ContextBag context)
            where TSagaData : class, IContainSagaData
        {
            return Get<TSagaData>(LearningSagaIdGenerator.Generate(typeof(TSagaData), propertyName, propertyValue), session);
        }

        public Task Complete(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
        {
            var storageSession = (LearningSynchronizedStorageSession)session;
            return storageSession.Complete(sagaData);
        }

        static Task<TSagaData> Get<TSagaData>(Guid sagaId, SynchronizedStorageSession session) where TSagaData : class, IContainSagaData
        {
            var storageSession = (LearningSynchronizedStorageSession)session;
            return storageSession.Read<TSagaData>(sagaId);
        }
    }
}