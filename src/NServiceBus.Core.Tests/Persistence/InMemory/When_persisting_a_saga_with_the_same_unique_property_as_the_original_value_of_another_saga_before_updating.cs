namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using NServiceBus.Saga;
    using NUnit.Framework;

    [TestFixture]
    class When_persisting_a_saga_with_the_same_unique_property_as_the_original_value_of_another_saga_before_updating
    {
        [Test]
        public void It_should_persist_successfully()
        {
            var saga1 = new SagaWithUniquePropertyData{Id = Guid.NewGuid(), UniqueString = "whatever"};
            var saga2 = new SagaWithUniquePropertyData{Id = Guid.NewGuid(), UniqueString = "whatever"};

            var options = new SagaPersistenceOptions(SagaMetadata.Create(typeof(SagaWithUniqueProperty)));

            var persister = InMemoryPersisterBuilder.Build<SagaWithUniqueProperty>();
            persister.Save(saga1, options);
            saga1 = persister.Get<SagaWithUniquePropertyData>(saga1.Id.ToString(), options);
            saga1.UniqueString = "whatever2";
            persister.Update(saga1, options);

            persister.Save(saga2, options);
        }
    }
}