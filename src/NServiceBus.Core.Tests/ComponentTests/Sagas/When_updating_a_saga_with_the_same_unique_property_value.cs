namespace NServiceBus.Persistence.ComponentTests
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    public class When_updating_a_saga_with_the_same_unique_property_value : SagaPersisterTests
    {
        [Test]
        public async Task It_should_persist_successfully()
        {
            var sagaId = Guid.NewGuid();
            var saga1 = new SagaWithCorrelationPropertyData
            {
                Id = sagaId,
                CorrelatedProperty = sagaId.ToString()
            };

            var persister = configuration.SagaStorage;
            var insertContextBag = configuration.GetContextBagForSagaStorage();
            using (var insertSession = await configuration.SynchronizedStorage.OpenSession(insertContextBag))
            {
                var correlationProperty = SetActiveSagaInstance(insertContextBag, new SagaWithCorrelationProperty(), saga1);

                await persister.Save(saga1, correlationProperty, insertSession, insertContextBag);
                await insertSession.CompleteAsync();
            }

            var updatingContext = configuration.GetContextBagForSagaStorage();
            using (var updateSession = await configuration.SynchronizedStorage.OpenSession(updatingContext))
            {
                saga1 = await persister.Get<SagaWithCorrelationPropertyData>(nameof(SagaWithCorrelationPropertyData.CorrelatedProperty), sagaId.ToString(), updateSession, updatingContext);

                SetActiveSagaInstance(updatingContext, new SagaWithCorrelationProperty(), saga1);

                await persister.Update(saga1, updateSession, updatingContext);
                await updateSession.CompleteAsync();
            }
        }
    }
}