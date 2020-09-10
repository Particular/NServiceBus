namespace NServiceBus.PersistenceTesting.Sagas
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using NServiceBus.Sagas;
    using NUnit.Framework;
    using Persistence;

    [TestFixtureSource(typeof(PersistenceTestsConfiguration), "SagaVariants")]
    public class SagaPersisterTests
    {
        public SagaPersisterTests(TestVariant param)
        {
            this.param = param;
        }

        [OneTimeSetUp]
        public virtual async Task OneTimeSetUp()
        {
            configuration = new PersistenceTestsConfiguration(param);
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
            using (var insertSession = await configuration.SynchronizedStorage.OpenSession(insertContextBag, CancellationToken.None))
            {
                await SaveSagaWithSession(saga, insertSession, insertContextBag);
                await insertSession.CompleteAsync(CancellationToken.None);
            }
        }

        protected async Task SaveSagaWithSession<TSagaData>(TSagaData saga, CompletableSynchronizedStorageSession session, ContextBag context)
            where TSagaData : class, IContainSagaData, new()
        {
            SetupNewSaga(saga);
            var correlationProperty = GetSagaCorrelationProperty(saga);
            await configuration.SagaStorage.Save(saga, correlationProperty, session, context, CancellationToken.None);
        }

        protected async Task<TSagaData> GetByCorrelationProperty<TSagaData>(string correlatedPropertyName, object correlationPropertyData) where TSagaData : class, IContainSagaData, new()
        {
            var context = configuration.GetContextBagForSagaStorage();
            TSagaData sagaData;
            var persister = configuration.SagaStorage;

            using (var completeSession = await configuration.SynchronizedStorage.OpenSession(context, CancellationToken.None))
            {
                sagaData = await persister.Get<TSagaData>(correlatedPropertyName, correlationPropertyData, completeSession, context, CancellationToken.None);

                await completeSession.CompleteAsync(CancellationToken.None);
            }

            return sagaData;
        }

        protected async Task<TSagaData> GetById<TSagaData>(Guid sagaId) where TSagaData : class, IContainSagaData, new()
        {
            var readContextBag = configuration.GetContextBagForSagaStorage();
            TSagaData sagaData;
            using (var readSession = await configuration.SynchronizedStorage.OpenSession(readContextBag, CancellationToken.None))
            {
                sagaData = await configuration.SagaStorage.Get<TSagaData>(sagaId, readSession, readContextBag, CancellationToken.None);

                await readSession.CompleteAsync(CancellationToken.None);
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

        void SetupNewSaga<TSagaData>(TSagaData sagaData) where TSagaData : IContainSagaData
        {
            if (sagaData.Id == Guid.Empty)
            {
                var correlationProperty = SagaCorrelationProperty.None;
                var sagaMetadata = configuration.SagaMetadataCollection.FindByEntity(typeof(TSagaData));
                if (sagaMetadata.TryGetCorrelationProperty(out var correlatedProp))
                {
                    var prop = sagaData.GetType().GetProperty(correlatedProp.Name);
                    var value = prop.GetValue(sagaData);
                    correlationProperty = new SagaCorrelationProperty(correlatedProp.Name, value);
                }

                sagaData.Id = configuration.SagaIdGenerator.Generate(new SagaIdGeneratorContext(correlationProperty, sagaMetadata, new ContextBag()));
            }

            if (sagaData.OriginalMessageId == null)
            {
                sagaData.OriginalMessageId = Guid.NewGuid().ToString("D");
            }
        }

        protected IPersistenceTestsConfiguration configuration;
        protected TestVariant param;
    }
}