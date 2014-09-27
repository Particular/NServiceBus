namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using NUnit.Framework;

    [TestFixture]
    class When_persisting_different_sagas_with_unique_properties : InMemorySagaPersistenceFixture
    {
        public When_persisting_different_sagas_with_unique_properties()
        {
            RegisterSaga<SagaWithTwoUniqueProperties>();
            RegisterSaga<AnotherSagaWithTwoUniqueProperties>();
            RegisterSaga<SagaWithUniqueProperty>();
        }
        [Test]
        public void  It_should_persist_successfully()
        {
            var saga1 = new SagaWithTwoUniquePropertiesData { Id = Guid.NewGuid(), UniqueString = "whatever", UniqueInt = 5 };
            var saga2 = new AnotherSagaWithTwoUniquePropertiesData { Id = Guid.NewGuid(), UniqueString = "whatever", UniqueInt = 5 };
            var saga3 = new SagaWithUniquePropertyData {Id = Guid.NewGuid(), UniqueString = "whatever"};

           

            persister.Save(saga1);
            persister.Save(saga2);
            persister.Save(saga3);
        }
    }
}
