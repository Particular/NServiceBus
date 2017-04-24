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
            configuration.RequiresFindersSupport();

            var propertyData = Guid.NewGuid().ToString();
            var saga1 = new SagaWithoutCorrelationPropertyData
            {
                FoundByFinderProperty = propertyData,
                DateTimeProperty = DateTime.UtcNow
            };
            var saga2 = new AnotherSagaWithoutCorrelationPropertyData
            {
                FoundByFinderProperty = propertyData
            };

            var persister = configuration.SagaStorage;
            var savingContextBag = configuration.GetContextBagForSagaStorage();
            using (var session = await configuration.SynchronizedStorage.OpenSession(savingContextBag))
            {
                var correlationPropertyNoneSaga1 = SetActiveSagaInstanceForSave(savingContextBag, new SagaWithoutCorrelationProperty(), saga1, typeof(CustomFinder));
                await persister.Save(saga1, correlationPropertyNoneSaga1, session, savingContextBag);

                var correlationPropertyNoneSaga2 = SetActiveSagaInstanceForSave(savingContextBag, new AnotherSagaWithoutCorrelationProperty(), saga2, typeof(AnotherCustomFinder));
                await persister.Save(saga2, correlationPropertyNoneSaga2, session, savingContextBag);

                await session.CompleteAsync();
            }

            var readContextBag = configuration.GetContextBagForSagaStorage();
            using (var readSession = await configuration.SynchronizedStorage.OpenSession(readContextBag))
            {
                SetActiveSagaInstanceForGet<SagaWithoutCorrelationProperty, SagaWithoutCorrelationPropertyData>(readContextBag, saga1, typeof(CustomFinder));
                var saga1Result = await persister.Get<SagaWithoutCorrelationPropertyData>(saga1.Id, readSession, readContextBag);

                SetActiveSagaInstanceForGet<AnotherSagaWithoutCorrelationProperty, AnotherSagaWithoutCorrelationPropertyData>(readContextBag, saga2, typeof(AnotherCustomFinder));
                var saga2Result = await persister.Get<AnotherSagaWithoutCorrelationPropertyData>(saga2.Id, readSession, readContextBag);

                Assert.AreEqual(saga1.FoundByFinderProperty, saga1Result.FoundByFinderProperty);
                Assert.AreEqual(saga2.FoundByFinderProperty, saga2Result.FoundByFinderProperty);
            }
        }
    }
}