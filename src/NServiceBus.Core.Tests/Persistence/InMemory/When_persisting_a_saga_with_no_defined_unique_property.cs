namespace NServiceBus.Core.Tests.Persistence.InMemory
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using NUnit.Framework;

    [TestFixture]
    public class When_persisting_a_saga_with_no_defined_unique_property
    {
        [Test]
        public async Task It_should_persist_successfully()
        {
            var sagaData = new SagaWithoutUniquePropertyData
            {
                Id = Guid.NewGuid(),
                NonUniqueString = "whatever"
            };

            var persister = new InMemorySagaPersister();
            var session = new InMemorySynchronizedStorageSession();

            await persister.Save(sagaData, null, session, new ContextBag());

            await session.CompleteAsync();
        }
    }
}