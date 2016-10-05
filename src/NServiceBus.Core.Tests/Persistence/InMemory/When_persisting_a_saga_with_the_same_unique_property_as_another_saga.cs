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
            var winningSession = new InMemorySynchronizedStorageSession();
            var losingSession = new InMemorySynchronizedStorageSession();

            await persister.Save(saga1, SagaMetadataHelper.GetMetadata<SagaWithUniqueProperty>(saga1), winningSession, new ContextBag());
            await persister.Save(saga2, SagaMetadataHelper.GetMetadata<SagaWithUniqueProperty>(saga1), losingSession, new ContextBag());

            await winningSession.CompleteAsync();

            Assert.That(async () => await losingSession.CompleteAsync(), Throws.InstanceOf<InvalidOperationException>());
        }
    }
}