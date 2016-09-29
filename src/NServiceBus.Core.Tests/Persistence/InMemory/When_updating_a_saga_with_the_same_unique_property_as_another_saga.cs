namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using NUnit.Framework;

    [TestFixture]
    class When_updating_a_saga_with_the_same_unique_property_as_another_saga
    {
        [Test]
        public async Task It_should_persist_successfully()
        {
            var saga1 = new SagaWithUniquePropertyData
            {
                Id = Guid.NewGuid(),
                UniqueString = "whatever1"
            };
            var saga2 = new SagaWithUniquePropertyData
            {
                Id = Guid.NewGuid(),
                UniqueString = "whatever"
            };

            var persister = new InMemorySagaPersister();
            var session = new InMemorySynchronizedStorageSession();
            await persister.Save(saga1, SagaMetadataHelper.GetMetadata<SagaWithUniqueProperty>(saga1), session, new ContextBag());
            await persister.Save(saga2, SagaMetadataHelper.GetMetadata<SagaWithUniqueProperty>(saga2), session, new ContextBag());
            await session.CompleteAsync();
        }
    }
}