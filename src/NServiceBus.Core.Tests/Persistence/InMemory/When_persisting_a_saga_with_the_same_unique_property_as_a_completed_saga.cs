namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Saga;
    using NUnit.Framework;

    [TestFixture]
    class When_persisting_a_saga_with_the_same_unique_property_as_a_completed_saga
    {
        [Test]
        public async Task It_should_persist_successfully()
        {
            var saga1 = new SagaWithUniquePropertyData { Id = Guid.NewGuid(), UniqueString = "whatever" };
            var saga2 = new SagaWithUniquePropertyData { Id = Guid.NewGuid(), UniqueString = "whatever" };

            var persister = InMemoryPersisterBuilder.Build<SagaWithUniqueProperty>();
            var options = new SagaPersistenceOptions(SagaMetadata.Create(typeof(SagaWithUniqueProperty)));

            await persister.Save(saga1, options);
            await persister.Complete(saga1, options);
            await persister.Save(saga2, options);
            await persister.Complete(saga2, options);
            await persister.Save(saga1, options);
            await persister.Complete(saga1, options);
        }
    }
}