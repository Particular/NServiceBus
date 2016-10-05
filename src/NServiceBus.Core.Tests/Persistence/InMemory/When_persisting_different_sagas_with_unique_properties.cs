namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using NUnit.Framework;

    [TestFixture]
    class When_persisting_different_sagas_with_unique_properties
    {
        [Test]
        public async Task It_should_persist_successfully()
        {
             var saga1 = new SagaWithUniquePropertyData
            {
                Id = Guid.NewGuid(),
                UniqueString = "whatever"
            };
            var saga2 = new AnotherSagaWithUniquePropertyData
            {
                Id = Guid.NewGuid(),
                UniqueString = "whatever"
            };

            var persister = new InMemorySagaPersister();
            var transaction = new InMemorySynchronizedStorageSession();
            await persister.Save(saga1, SagaMetadataHelper.GetMetadata<SagaWithUniqueProperty>(saga1), transaction, new ContextBag());
            await persister.Save(saga2, SagaMetadataHelper.GetMetadata<AnotherSagaTwoUniqueProperty>(saga2), transaction, new ContextBag());
            await transaction.CompleteAsync();
        }
    }
}