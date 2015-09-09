namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Saga;
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
            var persister = InMemoryPersisterBuilder.Build<SagaWithUniqueProperty>();
            var options = new SagaPersistenceOptions(SagaMetadata.Create(typeof(SagaWithUniqueProperty)));

            await persister.Save(saga1, options);
            saga1 = await persister.Get<SagaWithUniquePropertyData>(saga1.Id, options);
            await persister.Update(saga1, options);
        }
    }
}