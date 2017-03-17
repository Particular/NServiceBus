namespace NServiceBus.Persistence.ComponentTests
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    public class When_completing_a_saga_with_no_defined_correlation_property : SagaPersisterTests
    {
        /// <summary>
        /// There can be a saga that is only started by a message and then is driven by timeouts only.
        /// This kind of saga would not require to be correlated by any property. This test ensures that in-memory persistence covers this case and can handle this kind of sagas properly.
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task It_should_successfully_remove_the_saga()
        {
            var sagaId = Guid.NewGuid();

            var persister = configuration.SagaStorage;
            var savingContextBag = configuration.GetContextBagForSagaStorage();
            using (var savingSession = await configuration.SynchronizedStorage.OpenSession(savingContextBag))
            {
                var sagaData = new SagaWithoutCorrelationPropertyData { Id = sagaId, FoundByFinderProperty = sagaId.ToString() };
                var correlationPropertyNone = SetActiveSagaInstance(savingContextBag, new SagaWithoutCorrelationProperty(), sagaData, typeof(CustomFinder));

                await persister.Save(sagaData, correlationPropertyNone, savingSession, savingContextBag);
                await savingSession.CompleteAsync();
            }

            var completingContextBag = configuration.GetContextBagForSagaStorage();
            using (var completingSession = await configuration.SynchronizedStorage.OpenSession(completingContextBag))
            {
                var saga = await persister.Get<SagaWithoutCorrelationPropertyData>(sagaId, completingSession, completingContextBag);
                SetActiveSagaInstance(completingContextBag, new SagaWithoutCorrelationProperty(), saga, typeof(CustomFinder));

                await persister.Complete(saga, completingSession, completingContextBag);
                await completingSession.CompleteAsync();
            }

            var readContextBag = configuration.GetContextBagForSagaStorage();
            using (var readSession = await configuration.SynchronizedStorage.OpenSession(readContextBag))
            {
                SetActiveSagaInstance(readContextBag, new SagaWithoutCorrelationProperty(), new SagaWithoutCorrelationPropertyData { FoundByFinderProperty = sagaId.ToString() }, typeof(CustomFinder));

                var result = await persister.Get<SagaWithoutCorrelationPropertyData>(sagaId, readSession, readContextBag);
                Assert.That(result, Is.Null);
            }
        }
    }
}