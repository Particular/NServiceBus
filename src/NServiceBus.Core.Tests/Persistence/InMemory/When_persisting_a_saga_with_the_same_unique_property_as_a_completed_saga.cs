namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using NUnit.Framework;

    [TestFixture]
    class When_persisting_a_saga_with_the_same_unique_property_as_a_completed_saga:InMemorySagaPersistenceFixture
    {
        public When_persisting_a_saga_with_the_same_unique_property_as_a_completed_saga()
        {
            RegisterSaga<SagaWithUniqueProperty>();
        }
        [Test]
        public void It_should_persist_successfully()
        {
            var saga1 = new SagaWithUniquePropertyData { Id = Guid.NewGuid(), UniqueString = "whatever" };
            var saga2 = new SagaWithUniquePropertyData { Id = Guid.NewGuid(), UniqueString = "whatever" };


            persister.Save(saga1);
            persister.Complete(saga1);
            persister.Save(saga2);
            persister.Complete(saga2);
            persister.Save(saga1);
            persister.Complete(saga1);

        }
    }
}