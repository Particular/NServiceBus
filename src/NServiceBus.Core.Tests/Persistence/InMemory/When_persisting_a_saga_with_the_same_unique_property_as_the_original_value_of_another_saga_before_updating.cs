namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NUnit.Framework;

    [TestFixture]
    class When_persisting_a_saga_with_the_same_unique_property_as_the_original_value_of_another_saga_before_updating
    {
        [Test]
        public async Task It_should_persist_successfully()
        {
            var saga1 = new SagaWithUniquePropertyData
            {
                Id = Guid.NewGuid(),
                UniqueString = "whatever"
            };
            var saga2 = new SagaWithUniquePropertyData
            {
                Id = Guid.NewGuid(),
                UniqueString = "whatever"
            };

            var persister = new InMemorySagaPersister();
            await persister.Save(saga1, SagaMetadataHelper.GetMetadata<SagaWithUniqueProperty>(), new ContextBag());
            saga1 = await persister.Get<SagaWithUniquePropertyData>(saga1.Id, new ContextBag());
            saga1.UniqueString = "whatever2";
            await persister.Update(saga1, new ContextBag());

            await persister.Save(saga2, SagaMetadataHelper.GetMetadata<SagaWithUniqueProperty>(), new ContextBag());
        }
    }
}