namespace NServiceBus.SagaPersisters.Raven.Tests
{
    using System;
    using InMemory.Tests;
    using NServiceBus.InMemory.SagaPersister;
    using NUnit.Framework;

    [TestFixture]
    public class When_updating_a_saga_with_the_same_unique_property_value
    {
        [Test]
        public void It_should_persist_successfully()
        {
            var saga1 = new SagaWithUniqueProperty
                {
                    Id = Guid.NewGuid(),
                    UniqueString = "whatever"
                };

            var inMemorySagaPersister = new InMemorySagaPersister();
            inMemorySagaPersister.Save(saga1);
            saga1 = inMemorySagaPersister.Get<SagaWithUniqueProperty>(saga1.Id);
            inMemorySagaPersister.Update(saga1);
        }
    }
}