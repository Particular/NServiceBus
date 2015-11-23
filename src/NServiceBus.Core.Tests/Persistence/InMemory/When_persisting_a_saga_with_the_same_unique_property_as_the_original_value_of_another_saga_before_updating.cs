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

            var firstInsertSession = new InMemorySynchronizedStorageSession();
            await persister.Save(saga1, SagaMetadataHelper.GetMetadata<SagaWithUniqueProperty>(saga1), firstInsertSession, new ContextBag());
            await firstInsertSession.CompleteAsync();

            var updateSession = new InMemorySynchronizedStorageSession();
            saga1 = await persister.Get<SagaWithUniquePropertyData>(saga1.Id, updateSession, new ContextBag());
            saga1.UniqueString = "whatever2";
            await persister.Update(saga1, updateSession, new ContextBag());
            await updateSession.CompleteAsync();

            var secondInsertSession = new InMemorySynchronizedStorageSession();
            await persister.Save(saga2, SagaMetadataHelper.GetMetadata<SagaWithUniqueProperty>(saga2), secondInsertSession, new ContextBag());
            await secondInsertSession.CompleteAsync();
        }
    }
}