namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using NUnit.Framework;

    [TestFixture]
    class When_persisting_a_saga_with_the_same_unique_property_as_a_completed_saga
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

            using (var session1 = new InMemorySynchronizedStorageSession())
            {
                await persister.Save(saga1, SagaMetadataHelper.GetMetadata<SagaWithUniqueProperty>(saga1), session1, new ContextBag());
                await persister.Complete(saga1, session1, new ContextBag());
                await session1.CompleteAsync();
            }

            using (var session2 = new InMemorySynchronizedStorageSession())
            {
                await persister.Save(saga2, SagaMetadataHelper.GetMetadata<SagaWithUniqueProperty>(saga2), session2, new ContextBag());
                await persister.Complete(saga2, session2, new ContextBag());
                await session2.CompleteAsync();
            }

            using (var session3 = new InMemorySynchronizedStorageSession())
            {
                await persister.Save(saga1, SagaMetadataHelper.GetMetadata<SagaWithUniqueProperty>(saga1), session3, new ContextBag());
                await persister.Complete(saga1, session3, new ContextBag());
                await session3.CompleteAsync();
            }
        }
    }
}