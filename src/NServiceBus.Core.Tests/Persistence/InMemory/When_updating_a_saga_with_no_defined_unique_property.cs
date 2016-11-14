namespace NServiceBus.Core.Tests.Persistence.InMemory
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using NServiceBus.Sagas;
    using NUnit.Framework;

    [TestFixture]
    public class When_updating_a_saga_with_no_defined_unique_property
    {
        [Test]
        public async Task It_should_successfully_update_the_saga()
        {
            var id = Guid.NewGuid();
            var sagaData = new SagaWithoutUniquePropertyData
            {
                Id = id,
                NonUniqueString = "whatever"
            };

            var persister = new InMemorySagaPersister();

            // first session
            var session = new InMemorySynchronizedStorageSession();

            await persister.Save(sagaData, SagaCorrelationProperty.None, session, new ContextBag());

            await session.CompleteAsync();

            // second session
            var session2 = new InMemorySynchronizedStorageSession();

            var ctx = new ContextBag();
            var saga = await persister.Get<SagaWithoutUniquePropertyData>(id, session2, ctx);
            saga.NonUniqueString = "asdfasdf";

            await persister.Update(saga, session2, ctx);

            await session2.CompleteAsync();

            var result = await persister.Get<SagaWithoutUniquePropertyData>(sagaData.Id, new InMemorySynchronizedStorageSession(), new ContextBag());

            Assert.That(result, Is.Not.Null);
            Assert.That(result.NonUniqueString, Is.EqualTo("asdfasdf"));
        }
    }
}