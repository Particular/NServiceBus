namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using NServiceBus.Saga;
    using NUnit.Framework;

    [TestFixture]
    class When_persisting_different_sagas_with_unique_properties 
    {
        [Test]
        public void  It_should_persist_successfully()
        {
            var saga1 = new SagaWithTwoUniquePropertiesData { Id = Guid.NewGuid(), UniqueString = "whatever", UniqueInt = 5 };
            var saga2 = new AnotherSagaWithTwoUniquePropertiesData { Id = Guid.NewGuid(), UniqueString = "whatever", UniqueInt = 5 };
            var saga3 = new SagaWithUniquePropertyData { Id = Guid.NewGuid(), UniqueString = "whatever" };
            var metadata1 = SagaMetadata.Create(typeof(SagaWithTwoUniqueProperties));
            var metadata2 = SagaMetadata.Create(typeof(AnotherSagaWithTwoUniqueProperties));
            var metadata3 = SagaMetadata.Create(typeof(SagaWithUniqueProperty));
            var persister = InMemoryPersisterBuilder.Build(typeof(SagaWithTwoUniqueProperties), typeof(AnotherSagaWithTwoUniqueProperties), typeof(SagaWithUniqueProperty));
            persister.Save(metadata1, saga1);
            persister.Save(metadata2, saga2);
            persister.Save(metadata3, saga3);
        }
    }
}
