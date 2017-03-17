namespace NServiceBus.Persistence.ComponentTests
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using NUnit.Framework;
    using Sagas;

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

        protected static SagaCorrelationProperty SetActiveSagaInstance<TSaga, TSagaData>(ContextBag savingContextBag, TSaga saga, TSagaData sagaData, params Type[] availableTypes)
            where TSaga : Saga<TSagaData>
            where TSagaData : IContainSagaData, new()
        {
            var sagaMetadata = SagaMetadata.Create(typeof(TSaga), availableTypes, new Conventions());
            var sagaInstance = new ActiveSagaInstance(saga, sagaMetadata, () => DateTime.UtcNow);
            sagaInstance.AttachNewEntity(sagaData);
            savingContextBag.Set(sagaInstance);

            SagaMetadata.CorrelationPropertyMetadata correlatedProp;
            if (!sagaMetadata.TryGetCorrelationProperty(out correlatedProp))
            {
                return SagaCorrelationProperty.None;
            }
            var prop = sagaData.GetType().GetProperty(correlatedProp.Name);

            var value = prop.GetValue(sagaData);

            return new SagaCorrelationProperty(correlatedProp.Name, value);
        }
    }
}