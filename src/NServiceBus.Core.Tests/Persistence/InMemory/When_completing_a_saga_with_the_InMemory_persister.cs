namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
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

            await persister.Save(saga, SagaMetadataHelper.GetMetadata<TestSaga>(saga), new ContextBagImpl());
            var sagaData = await persister.Get<TestSagaData>(saga.Id, new ContextBagImpl());
            await persister.Complete(saga, new ContextBagImpl());
            var completedSaga = await persister.Get<TestSagaData>(saga.Id, new ContextBagImpl());

            Assert.NotNull(sagaData);
            Assert.Null(completedSaga);
        }
    }
}