namespace NServiceBus.Core.Tests.Persistence.InMemory
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using NUnit.Framework;

    [TestFixture]
    public class When_updating_a_saga_with_no_defined_unique_property
    {
        [Test]
        public async Task It_should_successfully_update_the_saga()
        {
            var sagaData = new SagaWithoutUniquePropertyData
            {
                Id = Guid.NewGuid(),
                NonUniqueString = "whatever"
            };

            var persister = new InMemorySagaPersister();
            var session = new InMemorySynchronizedStorageSession();

            await persister.Save(sagaData, null, session, new ContextBag());

            sagaData.NonUniqueString = "asdfasdf";

            await persister.Update(sagaData, session, new ContextBag());

            await session.CompleteAsync();

            var result = await persister.Get<SagaWithoutUniquePropertyData>(sagaData.Id, session, new ContextBag());

            Assert.That(result, Is.Not.Null);
            Assert.That(result.NonUniqueString, Is.EqualTo("asdfasdf"));
        }
    }
}