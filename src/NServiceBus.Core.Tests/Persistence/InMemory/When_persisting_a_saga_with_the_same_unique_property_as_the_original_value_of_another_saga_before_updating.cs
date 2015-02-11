namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using NUnit.Framework;

    [TestFixture]
    class When_persisting_a_saga_with_the_same_unique_property_as_the_original_value_of_another_saga_before_updating
    {
        [Test]
        public void It_should_persist_successfully()
        {
            var saga1 = new SagaWithUniquePropertyData{Id = Guid.NewGuid(), UniqueString = "whatever"};
            var saga2 = new SagaWithUniquePropertyData{Id = Guid.NewGuid(), UniqueString = "whatever"};

            var persister = InMemoryPersisterBuilder.Build<SagaWithUniqueProperty>();
            persister.Save(saga1);
            saga1 = persister.Get<SagaWithUniquePropertyData>(saga1.Id);
            saga1.UniqueString = "whatever2";
            persister.Update(saga1);

            persister.Save(saga2);
        }
    }
}