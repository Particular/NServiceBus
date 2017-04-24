// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
namespace NServiceBus.Persistence.ComponentTests
{
    using System;
    using Extensibility;
    using Sagas;

    public partial class PersistenceTestsConfiguration : IPersistenceTestsConfiguration
    {
        public Func<ContextBag> GetContextBagForTimeoutPersister { get; set; } = () => new ContextBag();
        public Func<ContextBag> GetContextBagForSagaStorage { get; set; } = () => new ContextBag();
        public Func<ContextBag> GetContextBagForOutbox { get; set; } = () => new ContextBag();
        public Func<ContextBag> GetContextBagForSubscriptions { get; set; } = () => new ContextBag();

        public SagaMetadataCollection SagaMetadataCollection
        {
            get{
                if (sagaMetadataCollection == null)
                {
                    sagaMetadataCollection = new SagaMetadataCollection();
                    sagaMetadataCollection.Initialize(new[]
                    {
                        typeof(AnotherSagaWithCorrelatedProperty),
                        typeof(AnotherSagaWithoutCorrelationProperty),
                        typeof(AnotherCustomFinder),
                        typeof(SagaWithComplexType),
                        typeof(SagaWithCorrelationProperty),
                        typeof(SagaWithoutCorrelationProperty),
                        typeof(CustomFinder),
                        typeof(SimpleSagaEntitySaga),
                        typeof(TestSaga)
                    });
                }
                return sagaMetadataCollection;
            }
            set { sagaMetadataCollection = value; }
        }

        SagaMetadataCollection sagaMetadataCollection;

    }
}