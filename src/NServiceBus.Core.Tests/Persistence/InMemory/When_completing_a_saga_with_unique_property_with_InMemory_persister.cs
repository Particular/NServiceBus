namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using NServiceBus.Saga;
    using NUnit.Framework;
    
    [TestFixture]
    class When_completing_a_saga_with_unique_property_with_InMemory_persister
    {
        SagaMetadata metadata;

        [SetUp]
        public void SetUp()
        {
            metadata = SagaMetadata.Create(typeof(SagaWithUniqueProperty));
        }

        [Test]
        public void Should_delete_the_saga()
        {
            var saga = new SagaWithUniquePropertyData { Id = Guid.NewGuid(), UniqueString = "whatever" };

            var persister = InMemoryPersisterBuilder.Build<SagaWithUniqueProperty>();
            persister.Save(metadata, saga);
            Assert.NotNull(persister.Get<SagaWithUniquePropertyData>(metadata, saga.Id));
            persister.Complete(metadata, saga);
            Assert.Null(persister.Get<SagaWithUniquePropertyData>(metadata, saga.Id));
        }
    }
}
