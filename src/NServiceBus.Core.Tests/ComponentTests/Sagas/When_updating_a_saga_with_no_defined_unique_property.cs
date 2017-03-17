namespace NServiceBus.Persistence.ComponentTests
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    public class When_updating_a_saga_with_no_defined_unique_property : SagaPersisterTests
    {
        [Test]
        public async Task It_should_successfully_update_the_saga()
        {
            var sagaId = Guid.NewGuid();
            var sagaData = new SagaWithoutCorrelationPropertyData
            {
                Id = sagaId,
                FoundByFinderProperty = sagaId.ToString()
            };

            var persister = configuration.SagaStorage;

            var savingContextBag = configuration.GetContextBagForSagaStorage();
            using (var session = await configuration.SynchronizedStorage.OpenSession(savingContextBag))
            {
                var correlationPropertyNone = SetActiveSagaInstance(savingContextBag, new SagaWithoutCorrelationProperty(), sagaData, typeof(CustomFinder));

                await persister.Save(sagaData, correlationPropertyNone, session, savingContextBag);
                await session.CompleteAsync();
            }

            // second session
            var updateValue = Guid.NewGuid().ToString();
            savingContextBag = configuration.GetContextBagForSagaStorage();
            using (var session = await configuration.SynchronizedStorage.OpenSession(savingContextBag))
            {
                var saga = await persister.Get<SagaWithoutCorrelationPropertyData>(sagaId, session, savingContextBag);
                saga.FoundByFinderProperty = updateValue;
                SetActiveSagaInstance(savingContextBag, new SagaWithoutCorrelationProperty(), saga, typeof(CustomFinder));

                await persister.Update(saga, session, savingContextBag);
                await session.CompleteAsync();
            }

            var readContextBag = configuration.GetContextBagForSagaStorage();
            SagaWithoutCorrelationPropertyData result;
            using (var readSession = await configuration.SynchronizedStorage.OpenSession(readContextBag))
            {
                result = await persister.Get<SagaWithoutCorrelationPropertyData>(sagaData.Id, readSession, readContextBag);
            }

            Assert.That(result, Is.Not.Null);
            Assert.That(result.FoundByFinderProperty, Is.EqualTo(updateValue));
        }
    }
}