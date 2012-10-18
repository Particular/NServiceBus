using System;
using NServiceBus.Saga;
using NServiceBus.SagaPersisters.InMemory;
using NServiceBus.SagaPersisters.InMemory.Tests;
using NServiceBus.Serializers.Binary;
using NUnit.Framework;

namespace NServiceBus.SagaPersisters.Raven.Tests
{
    public class When_updating_a_saga_with_the_same_unique_property_value
    {
        [TestFixtureSetUp]
        public void FixtureSetUp()
        {
            InMemorySagaPersister.ConfigureSerializer = () => { return new MessageSerializer(); };
        }

        [Test]
        public void It_should_persist_successfully()
        {
            var saga1 = new SagaWithUniqueProperty { Id = Guid.NewGuid(), UniqueString = "whatever" };

            var inMemorySagaPersister = new InMemorySagaPersister() as ISagaPersister;
            inMemorySagaPersister.Save(saga1);
            saga1 = inMemorySagaPersister.Get<SagaWithUniqueProperty>(saga1.Id);
            inMemorySagaPersister.Update(saga1);
        }
    }
}