namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Persistence;
    using Sagas;

    class LearningSagaPersister : ISagaPersister
    {
        public Task Save(IContainSagaData sagaData, SagaCorrelationProperty correlationProperty, SynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default)
        {
            var storageSession = (LearningSynchronizedStorageSession)session;
            storageSession.Save(sagaData);
            return Task.CompletedTask;
        }

        public Task Update(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default)
        {
            var storageSession = (LearningSynchronizedStorageSession)session;
            storageSession.Update(sagaData);
            return Task.CompletedTask;
        }

        public Task<TSagaData> Get<TSagaData>(Guid sagaId, SynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default)
            where TSagaData : class, IContainSagaData
        {
            return Get<TSagaData>(sagaId, session, cancellationToken);
        }

        public Task<TSagaData> Get<TSagaData>(string propertyName, object propertyValue, SynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default)
            where TSagaData : class, IContainSagaData
        {
            return Get<TSagaData>(LearningSagaIdGenerator.Generate(typeof(TSagaData), propertyName, propertyValue), session, cancellationToken);
        }

        public Task Complete(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default)
        {
            var storageSession = (LearningSynchronizedStorageSession)session;
            storageSession.Complete(sagaData);
            return Task.CompletedTask;
        }

        static Task<TSagaData> Get<TSagaData>(Guid sagaId, SynchronizedStorageSession session, CancellationToken cancellationToken) where TSagaData : class, IContainSagaData
        {
            var storageSession = (LearningSynchronizedStorageSession)session;
            return storageSession.Read<TSagaData>(sagaId, cancellationToken);
        }
    }
}