namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Sagas;
    using NUnit.Framework;
    
    [TestFixture]
    class When_completing_a_saga_with_unique_property_with_InMemory_persister
    {
        SagaPersistenceOptions options;

        [SetUp]
        public void SetUp()
        {
            options = new SagaPersistenceOptions(SagaMetadata.Create(typeof(SagaWithUniqueProperty)));
        }

        [Test]
        public async Task Should_delete_the_saga()
        {
            var saga = new SagaWithUniquePropertyData { Id = Guid.NewGuid(), UniqueString = "whatever" };

            var persister = InMemoryPersisterBuilder.Build<SagaWithUniqueProperty>();
            await persister.Save(saga, options);
            var sagaData = await persister.Get<SagaWithUniquePropertyData>(saga.Id, options);
            await persister.Complete(saga, options);
            var completedSagaData = await persister.Get<SagaWithUniquePropertyData>(saga.Id, options);

            Assert.NotNull(sagaData);
            Assert.Null(completedSagaData);
        }
    }
}
