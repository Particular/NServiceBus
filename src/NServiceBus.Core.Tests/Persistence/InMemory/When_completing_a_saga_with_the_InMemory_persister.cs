namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using NServiceBus.Saga;
    using NUnit.Framework;

    [TestFixture]
    class When_completing_a_saga_with_the_InMemory_persister
    {
        [Test]
        public void Should_delete_the_saga()
        {
            var saga = new TestSagaData { Id = Guid.NewGuid() };
            var persister = InMemoryPersisterBuilder.Build<TestSaga>();
            var options = new SagaPersistenceOptions(SagaMetadata.Create(typeof(TestSaga)));

            persister.Save(saga, options);
            Assert.NotNull(persister.Get<TestSagaData>(saga.Id, options));
            persister.Complete(saga, options);
            Assert.Null(persister.Get<TestSagaData>(saga.Id, options));
        }
    }
}
