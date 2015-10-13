namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using NUnit.Framework;

    [TestFixture]
    class When_persisting_a_saga_with_the_same_unique_property_as_another_saga
    {
        [Test]
        public async Task It_should_enforce_uniqueness()
        {
            var saga1 = new SagaWithUniquePropertyData { Id = Guid.NewGuid(), UniqueString = "whatever" };
            var saga2 = new SagaWithUniquePropertyData { Id = Guid.NewGuid(), UniqueString = "whatever" };

            var persister = new InMemorySagaPersister();
            await persister.Save(saga1, SagaMetadataHelper.GetMetadata<SagaWithUniqueProperty>(), new ContextBag());
            Assert.Throws<InvalidOperationException>(() => persister.Save(saga2, SagaMetadataHelper.GetMetadata<SagaWithUniqueProperty>(), new ContextBag()));
        }
        [Test]
        public async Task It_should_enforce_uniqueness_even_for_two_unique_properties()
        {
            var saga1 = new SagaWithTwoUniquePropertiesData { Id = Guid.NewGuid(), UniqueString = "whatever", UniqueInt = 5 };
            var saga2 = new SagaWithTwoUniquePropertiesData { Id = Guid.NewGuid(), UniqueString = "whatever1", UniqueInt = 3 };
            var saga3 = new SagaWithTwoUniquePropertiesData { Id = Guid.NewGuid(), UniqueString = "whatever3", UniqueInt = 3 };

            var persister = new InMemorySagaPersister();
            await persister.Save(saga1, SagaMetadataHelper.GetMetadata<SagaWithTwoUniqueProperties>(), new ContextBag());
            await persister.Save(saga2, SagaMetadataHelper.GetMetadata<SagaWithTwoUniqueProperties>(), new ContextBag());
            Assert.Throws<InvalidOperationException>(() => persister.Save(saga3, SagaMetadataHelper.GetMetadata<SagaWithTwoUniqueProperties>(), new ContextBag()));
        }

    }
}