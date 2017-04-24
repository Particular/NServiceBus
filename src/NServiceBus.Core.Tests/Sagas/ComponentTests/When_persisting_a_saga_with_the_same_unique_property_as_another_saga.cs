namespace NServiceBus.Persistence.ComponentTests
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    public class When_persisting_a_saga_with_the_same_unique_property_as_another_saga: SagaPersisterTests
    {
        [Test]
        public async Task It_should_enforce_uniqueness()
        {
            var correlationPropertyData = Guid.NewGuid().ToString();
            var saga1 = new SagaWithCorrelationPropertyData
            {
                CorrelatedProperty = correlationPropertyData,
                DateTimeProperty = DateTime.UtcNow
            };
            var saga2 = new SagaWithCorrelationPropertyData
            {
                CorrelatedProperty = correlationPropertyData,
                DateTimeProperty = DateTime.UtcNow
            };

            var persister = configuration.SagaStorage;

            var winningContextBag = configuration.GetContextBagForSagaStorage();
            using (var winningSession = await configuration.SynchronizedStorage.OpenSession(winningContextBag))
            {
                var correlationPropertySaga1 = SetActiveSagaInstanceForSave(winningContextBag, new SagaWithCorrelationProperty(), saga1);
                await persister.Save(saga1, correlationPropertySaga1, winningSession, winningContextBag);
                await winningSession.CompleteAsync();
            }

            var losingContextBag = configuration.GetContextBagForSagaStorage();
            using (var losingSession = await configuration.SynchronizedStorage.OpenSession(losingContextBag))
            {
                var correlationPropertySaga2 = SetActiveSagaInstanceForSave(losingContextBag, new SagaWithCorrelationProperty(), saga2);
                await persister.Save(saga2, correlationPropertySaga2, losingSession, losingContextBag);

                // ReSharper disable once AccessToDisposedClosure
                Assert.That(async () => await losingSession.CompleteAsync(), Throws.InstanceOf<Exception>().And.Message.EndsWith($"The saga with the correlation id 'Name: {nameof(SagaWithCorrelationPropertyData.CorrelatedProperty)} Value: {correlationPropertyData}' already exists."));
            }
        }
    }
}