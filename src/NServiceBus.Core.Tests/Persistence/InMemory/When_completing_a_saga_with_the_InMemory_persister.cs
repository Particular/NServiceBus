namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using NUnit.Framework;

    [TestFixture]
    class When_completing_a_saga_with_the_InMemory_persister:InMemorySagaPersistenceFixture
    {
        public When_completing_a_saga_with_the_InMemory_persister()
        {
            RegisterSaga<TestSaga>();
        }

        [Test]
        public void Should_delete_the_saga()
        {
            var saga = new TestSagaData { Id = Guid.NewGuid() };
            
            persister.Save(saga);
            Assert.NotNull(persister.Get<TestSagaData>(saga.Id));
            persister.Complete(saga);
            Assert.Null(persister.Get<TestSagaData>(saga.Id));
        }
    }
}
