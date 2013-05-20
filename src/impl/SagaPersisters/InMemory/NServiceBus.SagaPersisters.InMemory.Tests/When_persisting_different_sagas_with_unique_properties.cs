namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using NUnit.Framework;
    using Persistence.InMemory.SagaPersister;
    using Saga;

    [TestFixture]
    public class When_persisting_different_sagas_with_unique_properties
    {
        [Test]
        public void  It_should_persist_successfully()
        {
            var saga1 = new SagaWithTwoUniqueProperties { Id = Guid.NewGuid(), UniqueString = "whatever", UniqueInt = 5 };
            var saga2 = new AnotherSagaWithTwoUniqueProperties { Id = Guid.NewGuid(), UniqueString = "whatever", UniqueInt = 5 };
            var saga3 = new SagaWithUniqueProperty {Id = Guid.NewGuid(), UniqueString = "whatever"};
            
            var inMemorySagaPersister = new InMemorySagaPersister() as ISagaPersister;

            inMemorySagaPersister.Save(saga1);
            inMemorySagaPersister.Save(saga2);
            inMemorySagaPersister.Save(saga3);
        }
    }
}
