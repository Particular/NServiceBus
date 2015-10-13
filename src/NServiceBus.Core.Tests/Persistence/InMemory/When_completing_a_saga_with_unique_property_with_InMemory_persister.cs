namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using NUnit.Framework;
    
    [TestFixture]
    class When_completing_a_saga_with_unique_property_with_InMemory_persister
    {
        [Test]
        public async Task Should_delete_the_saga()
        {
            var saga = new SagaWithUniquePropertyData { Id = Guid.NewGuid(), UniqueString = "whatever" };

            var persister = new InMemorySagaPersister();
            await persister.Save(saga,SagaMetadataHelper.GetMetadata<SagaWithUniqueProperty>(), new ContextBag());
            var sagaData = await persister.Get<SagaWithUniquePropertyData>(saga.Id, new ContextBag());
            await persister.Complete(saga, new ContextBag());
            var completedSagaData = await persister.Get<SagaWithUniquePropertyData>(saga.Id, new ContextBag());

            Assert.NotNull(sagaData);
            Assert.Null(completedSagaData);
        }
    }
}
