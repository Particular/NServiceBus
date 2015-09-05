namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using NServiceBus.Saga;
    using NUnit.Framework;
    
    [TestFixture]
    class When_completing_a_saga_with_unique_property_with_InMemory_persister
    {
        SagaPersistenceOptions options;

        [SetUp]
        public void SetUp()
        {
            options = new SagaPersistenceOptions(SagaMetadata.Create(typeof(SagaWithUniqueProperty)));
        }

        [Test]
        public void Should_delete_the_saga()
        {
            var saga = new SagaWithUniquePropertyData { Id = Guid.NewGuid(), UniqueString = "whatever" };

            var persister = InMemoryPersisterBuilder.Build<SagaWithUniqueProperty>();
            persister.Save(saga, options);
            Assert.NotNull(persister.Get<SagaWithUniquePropertyData>(saga.Id.ToString(), options));
            persister.Complete(saga, options);
            Assert.Null(persister.Get<SagaWithUniquePropertyData>(saga.Id.ToString(), options));
        }
    }
}
