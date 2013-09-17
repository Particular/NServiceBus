namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using NUnit.Framework;
    using Persistence.InMemory.SagaPersister;
    using Saga;

    [TestFixture]
    public class When_persisting_a_saga_with_the_same_unique_property_as_the_original_value_of_another_saga_before_updating
    {
        [Test]
        public void It_should_persist_successfully()
        {
            var saga1 = new SagaWithUniqueProperty{Id = Guid.NewGuid(), UniqueString = "whatever"};
            var saga2 = new SagaWithUniqueProperty{Id = Guid.NewGuid(), UniqueString = "whatever"};
            var inMemorySagaPersister = new InMemorySagaPersister() as ISagaPersister;
            
            inMemorySagaPersister.Save(saga1);
            saga1 = inMemorySagaPersister.Get<SagaWithUniqueProperty>(saga1.Id);
            saga1.UniqueString = "whatever2";
            inMemorySagaPersister.Update(saga1);

            inMemorySagaPersister.Save(saga2);
        }
    }
}