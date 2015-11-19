namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NUnit.Framework;

    [TestFixture]
    class When_updating_a_saga_with_the_same_unique_property_value
    {
        [Test]
        public async Task It_should_persist_successfully()
        {
            var saga1 = new SagaWithUniquePropertyData
            {
                Id = Guid.NewGuid(),
                UniqueString = "whatever"
            };
            var persister = new InMemorySagaPersister();

            await persister.Save(saga1, SagaMetadataHelper.GetMetadata<SagaWithUniqueProperty>(saga1), new ContextBag());
            saga1 = await persister.Get<SagaWithUniquePropertyData>(saga1.Id, new ContextBag());
            await persister.Update(saga1, new ContextBag());
        }
    }
}