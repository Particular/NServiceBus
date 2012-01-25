using System;
using NServiceBus.Saga;
using NUnit.Framework;

namespace NServiceBus.SagaPersisters.InMemory.Tests
{
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
