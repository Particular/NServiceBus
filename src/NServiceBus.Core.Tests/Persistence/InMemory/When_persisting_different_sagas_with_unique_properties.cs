namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NUnit.Framework;

    [TestFixture]
    class When_persisting_different_sagas_with_unique_properties
    {
        [Test]
        public async Task It_should_persist_successfully()
        {
            var saga1 = new SagaWithTwoUniquePropertiesData
            {
                Id = Guid.NewGuid(),
                UniqueString = "whatever",
                UniqueInt = 5
            };
            var saga2 = new AnotherSagaWithTwoUniquePropertiesData
            {
                Id = Guid.NewGuid(),
                UniqueString = "whatever",
                UniqueInt = 5
            };
            var saga3 = new SagaWithUniquePropertyData
            {
                Id = Guid.NewGuid(),
                UniqueString = "whatever"
            };

            var persister = new InMemorySagaPersister();
            await persister.Save(saga1, SagaMetadataHelper.GetMetadata<SagaWithTwoUniqueProperties>(), new ContextBag());
            await persister.Save(saga2, SagaMetadataHelper.GetMetadata<AnotherSagaWithTwoUniqueProperties>(), new ContextBag());
            await persister.Save(saga3, SagaMetadataHelper.GetMetadata<SagaWithUniqueProperty>(), new ContextBag());
        }
    }
}