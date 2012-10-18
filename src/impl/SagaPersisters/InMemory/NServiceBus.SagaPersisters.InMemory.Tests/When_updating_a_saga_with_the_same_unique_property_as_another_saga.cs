using System;
using NServiceBus.Saga;
using NServiceBus.SagaPersisters.InMemory;
using NServiceBus.SagaPersisters.InMemory.Tests;
using NServiceBus.Serializers.Binary;
using NUnit.Framework;

namespace NServiceBus.SagaPersisters.Raven.Tests
{
    public class When_updating_a_saga_with_the_same_unique_property_as_another_saga
    {
        [TestFixtureSetUp]
        public void FixtureSetUp()
        {
            InMemorySagaPersister.ConfigureSerializer = () => { return new MessageSerializer(); };
        }

        [Test]
        public void It_should_persist_successfully()
        {
            var saga1 = new SagaWithUniqueProperty { Id = Guid.NewGuid(), UniqueString = "whatever1" };
            var saga2 = new SagaWithUniqueProperty { Id = Guid.NewGuid(), UniqueString = "whatever" };
            var inMemorySagaPersister = new InMemorySagaPersister() as ISagaPersister;

            inMemorySagaPersister.Save(saga1);
            inMemorySagaPersister.Save(saga2);

            Assert.Throws<InvalidOperationException>(() =>
            {
                var saga = inMemorySagaPersister.Get<SagaWithUniqueProperty>(saga2.Id);
                saga.UniqueString = "whatever1";
                inMemorySagaPersister.Update(saga);
            });
        }

        [Test]
        public void It_should_persist_successfully_for_two_unique_properties()
        {
            var saga1 = new SagaWithTwoUniqueProperties { Id = Guid.NewGuid(), UniqueString = "whatever1", UniqueInt = 5 };
            var saga2 = new SagaWithTwoUniqueProperties { Id = Guid.NewGuid(), UniqueString = "whatever", UniqueInt = 37 };
            var inMemorySagaPersister = new InMemorySagaPersister() as ISagaPersister;

            inMemorySagaPersister.Save(saga1);
            inMemorySagaPersister.Save(saga2);

            Assert.Throws<InvalidOperationException>(() =>
            {
                var saga = inMemorySagaPersister.Get<SagaWithTwoUniqueProperties>(saga2.Id);
                saga.UniqueInt = 5;
                inMemorySagaPersister.Update(saga);
            });
        }
    }
}