namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using NUnit.Framework;

    [TestFixture]
    class When_persisting_a_saga_with_the_same_unique_property_as_another_saga:InMemorySagaPersistenceFixture
    {
        public When_persisting_a_saga_with_the_same_unique_property_as_another_saga()
        {
            RegisterSaga<SagaWithUniqueProperty>();
            RegisterSaga<SagaWithTwoUniqueProperties>();
        }
        [Test]
        public void It_should_enforce_uniqueness()
        {
            var saga1 = new SagaWithUniquePropertyData { Id = Guid.NewGuid(), UniqueString = "whatever"};
            var saga2 = new SagaWithUniquePropertyData { Id = Guid.NewGuid(), UniqueString = "whatever"};

            persister.Save(saga1);
            Assert.Throws<InvalidOperationException>(() => persister.Save(saga2));
        }
        [Test]
        public void It_should_enforce_uniqueness_even_for_two_unique_properties()
        {
            var saga1 = new SagaWithTwoUniquePropertiesData { Id = Guid.NewGuid(), UniqueString = "whatever", UniqueInt = 5};
            var saga2 = new SagaWithTwoUniquePropertiesData { Id = Guid.NewGuid(), UniqueString = "whatever1", UniqueInt = 3};
            var saga3 = new SagaWithTwoUniquePropertiesData { Id = Guid.NewGuid(), UniqueString = "whatever3", UniqueInt = 3 };
          
            persister.Save(saga1);
            persister.Save(saga2);
            Assert.Throws<InvalidOperationException>(() => persister.Save(saga3));
        }

    }
}