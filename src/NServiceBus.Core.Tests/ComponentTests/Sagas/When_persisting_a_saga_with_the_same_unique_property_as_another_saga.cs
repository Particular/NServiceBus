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
            var saga1Id = Guid.NewGuid();
            var saga1 = new SagaWithCorrelationPropertyData
            {
                Id = saga1Id,
                CorrelatedProperty = saga1Id.ToString()
            };
            var saga2 = new SagaWithCorrelationPropertyData
            {
                Id = Guid.NewGuid(),
                CorrelatedProperty = saga1Id.ToString()
            };

            var persister = configuration.SagaStorage;

            var winningContextBag = configuration.GetContextBagForSagaStorage();
            var winningSession = await configuration.SynchronizedStorage.OpenSession(winningContextBag);
            var correlationPropertySaga1 = SetActiveSagaInstance(winningContextBag, new SagaWithCorrelationProperty(), saga1);
            await persister.Save(saga1, correlationPropertySaga1, winningSession, winningContextBag);
            await winningSession.CompleteAsync();

            var losingContextBag = configuration.GetContextBagForSagaStorage();
            var losingSession = await configuration.SynchronizedStorage.OpenSession(losingContextBag);
            var correlationPropertySaga2 = SetActiveSagaInstance(losingContextBag, new SagaWithCorrelationProperty(), saga2);
            await persister.Save(saga2, correlationPropertySaga2, losingSession, losingContextBag);

            Assert.That(async () => await losingSession.CompleteAsync(), Throws.InstanceOf<Exception>().And.Message.EndsWith($"The saga with the correlation id 'Name: {nameof(SagaWithCorrelationPropertyData.CorrelatedProperty)} Value: {saga1Id}' already exists."));
        }
    }
}