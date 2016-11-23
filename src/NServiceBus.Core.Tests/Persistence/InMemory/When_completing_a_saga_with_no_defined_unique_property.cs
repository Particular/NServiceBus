namespace NServiceBus.Core.Tests.Persistence.InMemory
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using NUnit.Framework;

    [TestFixture]
    public class When_completing_a_saga_with_no_defined_unique_property
    {
        /// <summary>
        /// There can be a saga that is only started by a message and then is driven by timeouts only. 
        /// This kind of saga would not require to be correlated by any property. This test ensures that in-memory persistence covers this case and can handle this kind of sagas properly. 
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task It_should_successfully_remove_the_saga()
        {
            var id = Guid.NewGuid();
            var sagaData = new SagaWithoutUniquePropertyData
            {
                Id = id,
                NonUniqueString = "whatever"
            };

            var persister = new InMemorySagaPersister();
            var savingSession = new InMemorySynchronizedStorageSession();

            await persister.Save(sagaData, null, savingSession, new ContextBag());
            await savingSession.CompleteAsync();

            // second session
            var completingSession = new InMemorySynchronizedStorageSession();
            var completingContextBag = new ContextBag();

            var saga = await persister.Get<SagaWithoutUniquePropertyData>(id, completingSession, completingContextBag);
            await persister.Complete(saga, completingSession, completingContextBag);
            await completingSession.CompleteAsync();

            var result = await persister.Get<SagaWithoutUniquePropertyData>(sagaData.Id, savingSession, new ContextBag());

            Assert.That(result, Is.Null);
        }
    }
}