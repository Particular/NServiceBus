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
            var insertSession = new InMemorySynchronizedStorageSession();
            await persister.Save(saga,SagaMetadataHelper.GetMetadata<SagaWithUniqueProperty>(saga), insertSession, new ContextBag());
            await insertSession.CompleteAsync();

            var intentionallySharedContext = new ContextBag();
            var sagaData = await persister.Get<SagaWithUniquePropertyData>(saga.Id, new InMemorySynchronizedStorageSession(), intentionallySharedContext );

            var completeSession = new InMemorySynchronizedStorageSession();
            await persister.Complete(saga, completeSession, intentionallySharedContext );
            await completeSession.CompleteAsync();

            var completedSagaData = await persister.Get<SagaWithUniquePropertyData>(saga.Id, new InMemorySynchronizedStorageSession(), new ContextBag());

            Assert.NotNull(sagaData);
            Assert.Null(completedSagaData);
        }
    }
}
