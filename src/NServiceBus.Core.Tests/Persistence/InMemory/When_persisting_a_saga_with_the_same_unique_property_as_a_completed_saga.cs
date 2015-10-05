namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using NUnit.Framework;

    [TestFixture]
    class When_persisting_a_saga_with_the_same_unique_property_as_a_completed_saga
    {
        [Test]
        public async Task It_should_persist_successfully()
        {
            var saga1 = new SagaWithUniquePropertyData { Id = Guid.NewGuid(), UniqueString = "whatever" };
            var saga2 = new SagaWithUniquePropertyData { Id = Guid.NewGuid(), UniqueString = "whatever" };

            var persister = InMemoryPersisterBuilder.Build<SagaWithUniqueProperty>();
         
            await persister.Save(saga1, new ContextBag());
            await persister.Complete(saga1, new ContextBag());
            await persister.Save(saga2, new ContextBag());
            await persister.Complete(saga2, new ContextBag());
            await persister.Save(saga1, new ContextBag());
            await persister.Complete(saga1, new ContextBag());
        }
    }
}