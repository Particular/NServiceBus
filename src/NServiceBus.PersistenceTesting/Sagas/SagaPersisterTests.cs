﻿namespace NServiceBus.PersistenceTesting.Sagas
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
        protected Task SaveSaga(TSagaData saga, params Type[] availableTypes) => SaveSaga<TSaga, TSagaData>(saga, availableTypes);
        protected Task<TSagaData> GetById(Guid sagaId, params Type[] availableTypes) => GetById<TSaga, TSagaData>(sagaId, availableTypes);
        protected Task<TSagaData> GetByCorrelationProperty(string correlatedPropertyName, object correlationPropertyData) => GetByCorrelationProperty<TSaga, TSagaData>(correlatedPropertyName, correlationPropertyData);

        // TODO: should be inlined as they don't reflect Core api's
        protected Task<TSagaData> GetByIdAndComplete(Guid sagaId, params Type[] availableTypes) => GetByIdAndComplete<TSaga, TSagaData>(sagaId, availableTypes);
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

        protected async Task SaveSaga<TSaga, TSagaData>(TSagaData saga, params Type[] availableTypes)
            where TSaga : Saga<TSagaData>, new()
            where TSagaData : class, IContainSagaData, new()
        {
            var insertContextBag = configuration.GetContextBagForSagaStorage();
            using (var insertSession = await configuration.SynchronizedStorage.OpenSession(insertContextBag))
            {
                SetupNewSaga(saga);
                var correlationProperty = GetSagaCorrelationProperty(saga);

                await configuration.SagaStorage.Save(saga, correlationProperty, insertSession, insertContextBag);
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

        protected async Task<TSagaData> GetByIdAndComplete<TSaga, TSagaData>(Guid sagaId, params Type[] availableTypes)
            where TSaga : Saga<TSagaData>, new()
            where TSagaData : class, IContainSagaData, new()
        {
            var context = configuration.GetContextBagForSagaStorage();
            TSagaData sagaData;
            var persister = configuration.SagaStorage;
            using (var completeSession = await configuration.SynchronizedStorage.OpenSession(context))
            {
                SetActiveSagaInstanceForGet<TSaga, TSagaData>(context, new TSagaData(), availableTypes);
                sagaData = await persister.Get<TSagaData>(sagaId, completeSession, context);
                SetActiveSagaInstanceForGet<TSaga, TSagaData>(context, sagaData, availableTypes);

                await persister.Complete(sagaData, completeSession, context);
                await completeSession.CompleteAsync();
            }

            return sagaData;
        }

        protected async Task<TSagaData> GetByCorrelationProperty<TSaga, TSagaData>(string correlatedPropertyName, object correlationPropertyData)
            where TSaga : Saga<TSagaData>, new()
            where TSagaData : class, IContainSagaData, new()
        {
            var context = configuration.GetContextBagForSagaStorage();
            TSagaData sagaData;
            var persister = configuration.SagaStorage;

            using (var completeSession = await configuration.SynchronizedStorage.OpenSession(context))
            {
                SetActiveSagaInstanceForGet<TSaga, TSagaData>(context, new TSagaData());

                sagaData = await persister.Get<TSagaData>(correlatedPropertyName, correlationPropertyData, completeSession, context);

                await completeSession.CompleteAsync();
            }

            return sagaData;
        }

        protected async Task<TSagaData> GetById<TSaga, TSagaData>(Guid sagaId, params Type[] availableTypes)
            where TSaga : Saga<TSagaData>, new()
            where TSagaData : class, IContainSagaData, new()
        {
            var readContextBag = configuration.GetContextBagForSagaStorage();
            TSagaData sagaData;
            using (var readSession = await configuration.SynchronizedStorage.OpenSession(readContextBag))
            {
                SetActiveSagaInstanceForGet<TSaga, TSagaData>(readContextBag, new TSagaData(), availableTypes);
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

        protected void SetActiveSagaInstanceForGet<TSaga, TSagaData>(ContextBag context, TSagaData sagaData, params Type[] availableTypes)
            where TSaga : Saga<TSagaData>, new()
            where TSagaData : class, IContainSagaData, new()
        {
            var sagaMetadata = configuration.SagaMetadataCollection.FindByEntity(typeof(TSagaData));
            var sagaInstance = new ActiveSagaInstance(new TSaga(), sagaMetadata, () => DateTime.UtcNow);

            sagaInstance.AttachNewEntity(sagaData);
            context.Set(sagaInstance);
        }

        protected IPersistenceTestsConfiguration configuration;
    }
}