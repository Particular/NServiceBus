﻿namespace NServiceBus.PersistenceTesting.Sagas
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Persistence;
    using NServiceBus.Sagas;
    using NUnit.Framework;

    [TestFixtureSource(typeof(PersistenceTestsConfiguration), nameof(PersistenceTestsConfiguration.SagaVariants))]
    public class SagaPersisterTests
    {
        public SagaPersisterTests(TestVariant param)
        {
            this.param = param.DeepCopy();
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

        protected async Task SaveSaga<TSagaData>(TSagaData saga, CancellationToken cancellationToken = default) where TSagaData : class, IContainSagaData, new()
        {
            var insertContextBag = configuration.GetContextBagForSagaStorage();
            using (var insertSession = configuration.CreateStorageSession())
            {
                await insertSession.Open(insertContextBag, cancellationToken);

                await SaveSagaWithSession(saga, insertSession, insertContextBag, cancellationToken);
                await insertSession.CompleteAsync(cancellationToken);
            }
        }

        protected async Task SaveSagaWithSession<TSagaData>(TSagaData saga, ICompletableSynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default)
            where TSagaData : class, IContainSagaData, new()
        {
            SetupNewSaga(saga);
            var correlationProperty = GetSagaCorrelationProperty(saga);
            await configuration.SagaStorage.Save(saga, correlationProperty, session, context, cancellationToken);
        }

        protected async Task<TSagaData> GetByCorrelationProperty<TSagaData>(string correlatedPropertyName, object correlationPropertyData, CancellationToken cancellationToken = default) where TSagaData : class, IContainSagaData, new()
        {
            var context = configuration.GetContextBagForSagaStorage();
            TSagaData sagaData;
            var persister = configuration.SagaStorage;

            using (var completeSession = configuration.CreateStorageSession())
            {
                await completeSession.Open(context, cancellationToken);

                sagaData = await persister.Get<TSagaData>(correlatedPropertyName, correlationPropertyData, completeSession, context, cancellationToken);

                await completeSession.CompleteAsync(cancellationToken);
            }

            return sagaData;
        }

        protected async Task<TSagaData> GetById<TSagaData>(Guid sagaId, CancellationToken cancellationToken = default) where TSagaData : class, IContainSagaData, new()
        {
            var readContextBag = configuration.GetContextBagForSagaStorage();
            TSagaData sagaData;
            using (var readSession = configuration.CreateStorageSession())
            {
                await readSession.Open(readContextBag, cancellationToken);

                sagaData = await configuration.SagaStorage.Get<TSagaData>(sagaId, readSession, readContextBag, cancellationToken);

                await readSession.CompleteAsync(cancellationToken);
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

#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable IDE0074 // Use compound assignment
            if (sagaData.OriginalMessageId == null)
            {
                sagaData.OriginalMessageId = Guid.NewGuid().ToString("D");
            }
#pragma warning restore IDE0074 // Use compound assignment
#pragma warning restore IDE0079 // Remove unnecessary suppression
        }

        protected IPersistenceTestsConfiguration configuration;
        protected TestVariant param;
    }
}