namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using NServiceBus.Saga;
    using NUnit.Framework;

    [TestFixture]
    class When_updating_a_saga_with_the_same_unique_property_value
    {
        [Test]
        public void It_should_persist_successfully()
        {
            var saga1 = new SagaWithUniquePropertyData
                {
                    Id = Guid.NewGuid(),
                    UniqueString = "whatever"
                };
            var persister = InMemoryPersisterBuilder.Build<SagaWithUniqueProperty>();
            var metadata = SagaMetadata.Create(typeof(SagaWithUniqueProperty));

            persister.Save(metadata, saga1);
            saga1 = persister.Get<SagaWithUniquePropertyData>(metadata, saga1.Id);
            persister.Update(metadata, saga1);
        }
    }
}