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
            var metadata = SagaMetadata.Create(typeof(TestSaga));
            persister.Save(metadata, saga);
            Assert.NotNull(persister.Get<TestSagaData>(metadata, saga.Id));
            persister.Complete(metadata, saga);
            Assert.Null(persister.Get<TestSagaData>(metadata, saga.Id));
        }
    }
}
