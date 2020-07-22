namespace NServiceBus.PersistenceTesting.Sagas
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using NServiceBus.Sagas;
    using NUnit.Framework;
    using Persistence;

    public class SagaPersisterTests<TSaga, TSagaData> : SagaPersisterTests
        where TSaga : Saga<TSagaData>, new()
        where TSagaData : class, IContainSagaData, new()
    {
        protected Task SaveSaga(TSagaData saga) => SaveSaga<TSagaData>(saga);
        protected Task<TSagaData> GetById(Guid sagaId) => GetById<TSagaData>(sagaId);
        protected Task<TSagaData> GetByCorrelationProperty(string correlatedPropertyName, object correlationPropertyData) => GetByCorrelationProperty<TSagaData>(correlatedPropertyName, correlationPropertyData);
    }

    public class SagaPersisterTests
    {
        [OneTimeSetUp]
        public virtual async Task OneTimeSetUp()
        {
            configuration = new PersistenceTestsConfiguration();
            await configuration.Configure();
        }

        [OneTimeTearDown]
        public virtual async Task OneTimeTearDown()
        {
            await configuration.Cleanup();
        }

        protected async Task SaveSaga<TSagaData>(TSagaData saga) where TSagaData : class, IContainSagaData, new()
        {
            var insertContextBag = configuration.GetContextBagForSagaStorage();
            using (var insertSession = await configuration.SynchronizedStorage.OpenSession(insertContextBag))
            {
                await SaveSagaWithSession(saga, insertSession, insertContextBag);
                await insertSession.CompleteAsync();
            }
        }

        protected async Task SaveSagaWithSession<TSagaData>(TSagaData saga, CompletableSynchronizedStorageSession session, ContextBag context)
            where TSagaData : class, IContainSagaData, new()
        {
            SetupNewSaga(saga);
            var correlationProperty = GetSagaCorrelationProperty(saga);
            await configuration.SagaStorage.Save(saga, correlationProperty, session, context);
        }

        protected async Task<TSagaData> GetByCorrelationProperty<TSagaData>(string correlatedPropertyName, object correlationPropertyData) where TSagaData : class, IContainSagaData, new()
        {
            var context = configuration.GetContextBagForSagaStorage();
            TSagaData sagaData;
            var persister = configuration.SagaStorage;

            using (var completeSession = await configuration.SynchronizedStorage.OpenSession(context))
            {
                sagaData = await persister.Get<TSagaData>(correlatedPropertyName, correlationPropertyData, completeSession, context);

                await completeSession.CompleteAsync();
            }

            return sagaData;
        }

        protected async Task<TSagaData> GetById<TSagaData>(Guid sagaId) where TSagaData : class, IContainSagaData, new()
        {
            var readContextBag = configuration.GetContextBagForSagaStorage();
            TSagaData sagaData;
            using (var readSession = await configuration.SynchronizedStorage.OpenSession(readContextBag))
            {
                sagaData = await configuration.SagaStorage.Get<TSagaData>(sagaId, readSession, readContextBag);

                await readSession.CompleteAsync();
            }

            return sagaData;
        }

        protected SagaCorrelationProperty GetSagaCorrelationProperty<TSagaData>(TSagaData sagaData)
        {
            var sagaMetadata = configuration.SagaMetadataCollection.FindByEntity(typeof(TSagaData));

            var correlationProperty = SagaCorrelationProperty.None;
            if (sagaMetadata.TryGetCorrelationProperty(out var correlatedProp))
            {
                var prop = sagaData.GetType().GetProperty(correlatedProp.Name);

                var value = prop.GetValue(sagaData);

                correlationProperty = new SagaCorrelationProperty(correlatedProp.Name, value);
            }

            return correlationProperty;
        }

        protected void SetupNewSaga<TSagaData>(TSagaData sagaData) where TSagaData : IContainSagaData
        {
            //TODO setup other IContainSagaData properties
            if (sagaData.Id == Guid.Empty)
            {
                var sagaMetadata = configuration.SagaMetadataCollection.FindByEntity(typeof(TSagaData));
                var correlationProperty = SagaCorrelationProperty.None;
                if (sagaMetadata.TryGetCorrelationProperty(out var correlatedProp))
                {
                    var prop = sagaData.GetType().GetProperty(correlatedProp.Name);

                    var value = prop.GetValue(sagaData);

                    correlationProperty = new SagaCorrelationProperty(correlatedProp.Name, value);
                }

                sagaData.Id = configuration.SagaIdGenerator.Generate(new SagaIdGeneratorContext(correlationProperty, sagaMetadata, new ContextBag()));
            }
        }

        protected IPersistenceTestsConfiguration configuration;
    }
}