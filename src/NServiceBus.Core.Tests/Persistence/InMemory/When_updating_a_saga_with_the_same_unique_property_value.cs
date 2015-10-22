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

            var insertSession = new InMemorySynchronizedStorageSession();
            await persister.Save(saga1, SagaMetadataHelper.GetMetadata<SagaWithUniqueProperty>(saga1), insertSession, new ContextBag());
            await insertSession.CompleteAsync();

            saga1 = await persister.Get<SagaWithUniquePropertyData>(saga1.Id, new InMemorySynchronizedStorageSession(), new ContextBag());

            var updateSession = new InMemorySynchronizedStorageSession();
            await persister.Update(saga1, updateSession, new ContextBag());
            await updateSession.CompleteAsync();
        }
    }
}