namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using NUnit.Framework;

    [TestFixture]
    class When_completing_a_saga_with_the_InMemory_persister
    {
        [Test]
        public async Task Should_delete_the_saga()
        {
            var saga = new TestSagaData
            {
                Id = Guid.NewGuid()
            };
            var persister = new InMemorySagaPersister();
            var insertSession = new InMemorySynchronizedStorageSession();
            await persister.Save(saga, SagaMetadataHelper.GetMetadata<TestSaga>(saga), insertSession, new ContextBag());
            await insertSession.CompleteAsync();

            var intentionallySharedContext = new ContextBag();
            var sagaData = await persister.Get<TestSagaData>(saga.Id, new InMemorySynchronizedStorageSession(), intentionallySharedContext );

            var deleteSession = new InMemorySynchronizedStorageSession();
            await persister.Complete(saga, deleteSession, intentionallySharedContext );
            await deleteSession.CompleteAsync();
            var completedSaga = await persister.Get<TestSagaData>(saga.Id, new InMemorySynchronizedStorageSession(), new ContextBag());

            Assert.NotNull(sagaData);
            Assert.Null(completedSaga);
        }
    }
}