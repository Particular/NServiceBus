namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using NUnit.Framework;

    [TestFixture]
    class When_persisting_different_sagas_with_unique_properties
    {
        [Test]
        public async Task It_should_persist_successfully()
        {
            var saga1 = new SagaWithTwoUniquePropertiesData
            {
                Id = Guid.NewGuid(),
                UniqueString = "whatever",
                UniqueInt = 5
            };
            var saga2 = new AnotherSagaWithTwoUniquePropertiesData
            {
                Id = Guid.NewGuid(),
                UniqueString = "whatever",
                UniqueInt = 5
            };
            var saga3 = new SagaWithUniquePropertyData
            {
                Id = Guid.NewGuid(),
                UniqueString = "whatever"
            };

            var persister = InMemoryPersisterBuilder.Build(typeof(SagaWithTwoUniqueProperties), typeof(AnotherSagaWithTwoUniqueProperties), typeof(SagaWithUniqueProperty));
            await persister.Save(saga1, new ContextBag());
            await persister.Save(saga2, new ContextBag());
            await persister.Save(saga3, new ContextBag());
        }
    }
}