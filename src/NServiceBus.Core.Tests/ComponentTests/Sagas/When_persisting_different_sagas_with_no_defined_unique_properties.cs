namespace NServiceBus.Persistence.ComponentTests
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    public class When_persisting_different_sagas_with_no_defined_unique_properties : SagaPersisterTests
    {
        [Test]
        public async Task It_should_persist_successfully()
        {
            var saga1Id = Guid.NewGuid();
            var saga1 = new SagaWithoutCorrelationPropertyData
            {
                Id = saga1Id,
                FoundByFinderProperty = saga1Id.ToString()
            };
            var saga2 = new AnotherSagaWithoutCorrelationPropertyData
            {
                Id = Guid.NewGuid(),
                FoundByFinderProperty = saga1Id.ToString()
            };

            var persister = configuration.SagaStorage;
            var savingContextBag = configuration.GetContextBagForSagaStorage();
            using (var session = await configuration.SynchronizedStorage.OpenSession(savingContextBag))
            {
                var correlationPropertyNoneSaga1 = SetActiveSagaInstance(savingContextBag, new SagaWithoutCorrelationProperty(), saga1, typeof(CustomFinder));
                await persister.Save(saga1, correlationPropertyNoneSaga1, session, savingContextBag);

                var correlationPropertyNoneSaga2 = SetActiveSagaInstance(savingContextBag, new AnotherSagaWithoutCorrelationProperty(), saga2, typeof(AnotherCustomFinder));
                await persister.Save(saga2, correlationPropertyNoneSaga2, session, savingContextBag);

                await session.CompleteAsync();
            }

            var readContextBag = configuration.GetContextBagForSagaStorage();
            using (var readSession = await configuration.SynchronizedStorage.OpenSession(readContextBag))
            {
                var saga1Result = await persister.Get<SagaWithoutCorrelationPropertyData>(saga1.Id, readSession, readContextBag);
                var saga2Result = await persister.Get<AnotherSagaWithoutCorrelationPropertyData>(saga2.Id, readSession, readContextBag);

                Assert.AreEqual(saga1.FoundByFinderProperty, saga1Result.FoundByFinderProperty);
                Assert.AreEqual(saga2.FoundByFinderProperty, saga2Result.FoundByFinderProperty);
            }
        }
    }
}