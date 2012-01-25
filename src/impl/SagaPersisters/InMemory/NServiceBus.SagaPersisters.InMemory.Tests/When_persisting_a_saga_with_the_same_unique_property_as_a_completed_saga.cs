using System;
using NServiceBus.Saga;
using NUnit.Framework;

namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    public class When_persisting_a_saga_with_the_same_unique_property_as_a_completed_saga
    {
        [Test]
        public void It_should_persist_successfully()
        {
            var saga1 = new SagaWithUniqueProperty { Id = Guid.NewGuid(), UniqueString = "whatever" };
            var saga2 = new SagaWithUniqueProperty { Id = Guid.NewGuid(), UniqueString = "whatever" };

             var inMemorySagaPersister = new InMemorySagaPersister() as ISagaPersister;

            inMemorySagaPersister.Save(saga1);
            inMemorySagaPersister.Complete(saga1);
            inMemorySagaPersister.Save(saga2);
            inMemorySagaPersister.Complete(saga2);
            inMemorySagaPersister.Save(saga1);
            inMemorySagaPersister.Complete(saga1);

        }
    }
}