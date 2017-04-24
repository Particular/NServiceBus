namespace NServiceBus.Persistence.ComponentTests
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using NUnit.Framework;
    using Sagas;

    public class SagaPersisterTests<TSaga, TSagaData> : SagaPersisterTests
        where TSaga : Saga<TSagaData>, new()
        where TSagaData : IContainSagaData, new()
    {
        protected Task SaveSaga(TSagaData saga, params Type[] availableTypes) => SaveSaga<TSaga, TSagaData>(saga, availableTypes);
        protected Task<TSagaData> GetByIdAndComplete(Guid sagaId, params Type[] availableTypes) => GetByIdAndComplete<TSaga, TSagaData>(sagaId, availableTypes);
        protected Task<TSagaData> GetByIdAndUpdate(Guid sagaId, Action<TSagaData> update, params Type[] availableTypes) => GetByIdAndUpdate<TSaga, TSagaData>(sagaId, update, availableTypes);
        protected Task<TSagaData> GetByCorrelationPropertyAndUpdate(string correlatedPropertyName, object correlationPropertyData, Action<TSagaData> update) => GetByCorrelationPropertyAndUpdate<TSaga, TSagaData>(correlatedPropertyName, correlationPropertyData, update);
        protected Task<TSagaData> GetByCorrelationPropertyAndComplete(string correlatedPropertyName, object correlationPropertyData) => GetByCorrelationPropertyAndComplete<TSaga, TSagaData>(correlatedPropertyName, correlationPropertyData);
        protected Task<TSagaData> GetByCorrelationProperty(string correlatedPropertyName, object correlationPropertyData) => GetByCorrelationProperty<TSaga, TSagaData>(correlatedPropertyName, correlationPropertyData);
        protected Task<TSagaData> GetById(Guid sagaId, params Type[] availableTypes) => GetById<TSaga, TSagaData>(sagaId, availableTypes);
    }

    public class SagaPersisterTests
    {
        protected PersistenceTestsConfiguration configuration;

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            configuration = new PersistenceTestsConfiguration();
            await configuration.Configure();
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            await configuration.Cleanup();
        }

        protected async Task SaveSaga<TSaga, TSagaData>(TSagaData saga, params Type[] availableTypes)
            where TSaga : Saga<TSagaData>, new()
            where TSagaData : IContainSagaData, new()
        {
            var insertContextBag = configuration.GetContextBagForSagaStorage();
            using (var insertSession = await configuration.SynchronizedStorage.OpenSession(insertContextBag))
            {
                var correlationProperty = SetActiveSagaInstanceForSave(insertContextBag, new TSaga(), saga, availableTypes);

                await configuration.SagaStorage.Save(saga, correlationProperty, insertSession, insertContextBag);
                await insertSession.CompleteAsync();
            }
        }

        protected async Task<TSagaData> GetByIdAndComplete<TSaga, TSagaData>(Guid sagaId, params Type[] availableTypes)
            where TSaga : Saga<TSagaData>, new()
            where TSagaData : IContainSagaData, new()
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

        protected async Task<TSagaData> GetByIdAndUpdate<TSaga, TSagaData>(Guid sagaId, Action<TSagaData> update, params Type[] availableTypes)
            where TSaga : Saga<TSagaData>, new()
            where TSagaData : IContainSagaData, new()
        {
            var context = configuration.GetContextBagForSagaStorage();
            TSagaData sagaData;
            var persister = configuration.SagaStorage;
            using (var completeSession = await configuration.SynchronizedStorage.OpenSession(context))
            {
                SetActiveSagaInstanceForGet<TSaga, TSagaData>(context, new TSagaData(), availableTypes);
                sagaData = await persister.Get<TSagaData>(sagaId, completeSession, context);
                SetActiveSagaInstanceForGet<TSaga, TSagaData>(context, sagaData, availableTypes);

                update(sagaData);

                await persister.Update(sagaData, completeSession, context);
                await completeSession.CompleteAsync();
            }
            return sagaData;
        }

        protected async Task<TSagaData> GetByCorrelationPropertyAndUpdate<TSaga, TSagaData>(string correlatedPropertyName, object correlationPropertyData, Action<TSagaData> update)
            where TSaga : Saga<TSagaData>, new()
            where TSagaData : IContainSagaData, new()
        {
            var context = configuration.GetContextBagForSagaStorage();
            TSagaData sagaData;
            var persister = configuration.SagaStorage;
            using (var completeSession = await configuration.SynchronizedStorage.OpenSession(context))
            {
                SetActiveSagaInstanceForGet<TSaga, TSagaData>(context, new TSagaData());

                sagaData = await persister.Get<TSagaData>(correlatedPropertyName, correlationPropertyData, completeSession, context);
                SetActiveSagaInstanceForGet<TSaga, TSagaData>(context, sagaData);

                update(sagaData);

                await persister.Update(sagaData, completeSession, context);
                await completeSession.CompleteAsync();
            }
            return sagaData;
        }

        protected async Task<TSagaData> GetByCorrelationPropertyAndComplete<TSaga, TSagaData>(string correlatedPropertyName, object correlationPropertyData)
           where TSaga : Saga<TSagaData>, new()
           where TSagaData : IContainSagaData, new()
        {
            var context = configuration.GetContextBagForSagaStorage();
            TSagaData sagaData;
            var persister = configuration.SagaStorage;
            using (var completeSession = await configuration.SynchronizedStorage.OpenSession(context))
            {
                SetActiveSagaInstanceForGet<TSaga, TSagaData>(context, new TSagaData());

                sagaData = await persister.Get<TSagaData>(correlatedPropertyName, correlationPropertyData, completeSession, context);
                SetActiveSagaInstanceForGet<TSaga, TSagaData>(context, sagaData);

                await persister.Complete(sagaData, completeSession, context);
                await completeSession.CompleteAsync();
            }
            return sagaData;
        }

        protected async Task<TSagaData> GetByCorrelationProperty<TSaga, TSagaData>(string correlatedPropertyName, object correlationPropertyData)
           where TSaga : Saga<TSagaData>, new()
           where TSagaData : IContainSagaData, new()
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
            where TSagaData : IContainSagaData, new()
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

        protected SagaCorrelationProperty SetActiveSagaInstanceForSave<TSaga, TSagaData>(ContextBag context, TSaga saga, TSagaData sagaData, params Type[] availableTypes)
            where TSaga : Saga<TSagaData>
            where TSagaData : IContainSagaData, new()
        {
            var sagaMetadata = configuration.SagaMetadataCollection.FindByEntity(typeof(TSagaData));
            var sagaInstance = new ActiveSagaInstance(saga, sagaMetadata, () => DateTime.UtcNow);
            var correlationProperty = SagaCorrelationProperty.None;
            SagaMetadata.CorrelationPropertyMetadata correlatedProp;
            if (sagaMetadata.TryGetCorrelationProperty(out correlatedProp))
            {
                var prop = sagaData.GetType().GetProperty(correlatedProp.Name);

                var value = prop.GetValue(sagaData);

                correlationProperty = new SagaCorrelationProperty(correlatedProp.Name, value);
            }

            if (sagaData.Id == Guid.Empty)
            {
                sagaData.Id = configuration.SagaIdGenerator.Generate(new SagaIdGeneratorContext(correlationProperty, sagaMetadata, context));
            }
            sagaInstance.AttachNewEntity(sagaData);
            context.Set(sagaInstance);

            return correlationProperty;
        }

        protected void SetActiveSagaInstanceForGet<TSaga, TSagaData>(ContextBag context, TSagaData sagaData, params Type[] availableTypes)
            where TSaga : Saga<TSagaData>, new ()
            where TSagaData : IContainSagaData, new()
        {
            var sagaMetadata = configuration.SagaMetadataCollection.FindByEntity(typeof(TSagaData));
            var sagaInstance = new ActiveSagaInstance(new TSaga(), sagaMetadata, () => DateTime.UtcNow);
        
            sagaInstance.AttachNewEntity(sagaData);
            context.Set(sagaInstance);
        }
    }
}