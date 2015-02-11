namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using NUnit.Framework;

    [TestFixture]
    class When_persisting_different_sagas_with_unique_properties 
    {
        [Test]
        public void  It_should_persist_successfully()
        {
            var saga1 = new SagaWithTwoUniquePropertiesData { Id = Guid.NewGuid(), UniqueString = "whatever", UniqueInt = 5 };
            var saga2 = new AnotherSagaWithTwoUniquePropertiesData { Id = Guid.NewGuid(), UniqueString = "whatever", UniqueInt = 5 };
            var saga3 = new SagaWithUniquePropertyData {Id = Guid.NewGuid(), UniqueString = "whatever"};
            var persister = InMemoryPersisterBuilder.Build(typeof(SagaWithTwoUniqueProperties), typeof(AnotherSagaWithTwoUniqueProperties), typeof(SagaWithUniqueProperty));
            persister.Save(saga1);
            persister.Save(saga2);
            persister.Save(saga3);
        }
    }
}
