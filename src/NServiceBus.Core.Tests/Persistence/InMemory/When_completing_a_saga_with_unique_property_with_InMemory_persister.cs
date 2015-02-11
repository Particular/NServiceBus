namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using NUnit.Framework;
    
    [TestFixture]
    class When_completing_a_saga_with_unique_property_with_InMemory_persister
    {
        [Test]
        public void Should_delete_the_saga()
        {
            var saga = new SagaWithUniquePropertyData { Id = Guid.NewGuid(), UniqueString = "whatever" };

            var persister = InMemoryPersisterBuilder.Build<SagaWithUniqueProperty>();
            persister.Save(saga);
            Assert.NotNull(persister.Get<SagaWithUniquePropertyData>(saga.Id));
            persister.Complete(saga);
            Assert.Null(persister.Get<SagaWithUniquePropertyData>(saga.Id));
        }
    }
}
