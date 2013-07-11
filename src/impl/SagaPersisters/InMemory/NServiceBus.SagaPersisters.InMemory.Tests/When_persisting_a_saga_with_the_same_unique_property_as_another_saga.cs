namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using NUnit.Framework;
    using Persistence.InMemory.SagaPersister;
    using Saga;

    [TestFixture]
    public class When_persisting_a_saga_with_the_same_unique_property_as_another_saga
    {
        [Test]
        public void It_should_enforce_uniqueness()
        {
            var saga1 = new SagaWithUniqueProperty { Id = Guid.NewGuid(), UniqueString = "whatever"};
            var saga2 = new SagaWithUniqueProperty { Id = Guid.NewGuid(), UniqueString = "whatever"};
            var inMemorySagaPersister = new InMemorySagaPersister() as ISagaPersister;

            inMemorySagaPersister.Save(saga1);
            Assert.Throws<InvalidOperationException>(() => inMemorySagaPersister.Save(saga2));
        }
        [Test]
        public void It_should_enforce_uniqueness_even_for_two_unique_properties()
        {
            var saga1 = new SagaWithTwoUniqueProperties() { Id = Guid.NewGuid(), UniqueString = "whatever", UniqueInt = 5};
            var saga2 = new SagaWithTwoUniqueProperties { Id = Guid.NewGuid(), UniqueString = "whatever1", UniqueInt = 3};
            var saga3 = new SagaWithTwoUniqueProperties { Id = Guid.NewGuid(), UniqueString = "whatever3", UniqueInt = 3 };
            var inMemorySagaPersister = new InMemorySagaPersister() as ISagaPersister;

            inMemorySagaPersister.Save(saga1);
            inMemorySagaPersister.Save(saga2);
            Assert.Throws<InvalidOperationException>(() => inMemorySagaPersister.Save(saga3));
        }

    }
}