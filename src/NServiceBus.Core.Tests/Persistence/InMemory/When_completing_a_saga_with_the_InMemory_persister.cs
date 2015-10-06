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
            var saga = new TestSagaData { Id = Guid.NewGuid() };
            var persister = InMemoryPersisterBuilder.Build<TestSaga>();
        
            await persister.Save(saga, new ContextBag());
            var sagaData = await persister.Get<TestSagaData>(saga.Id, new ContextBag());
            await persister.Complete(saga, new ContextBag());
            var completedSaga = await persister.Get<TestSagaData>(saga.Id, new ContextBag());

            Assert.NotNull(sagaData);
            Assert.Null(completedSaga);
        }
    }
}
