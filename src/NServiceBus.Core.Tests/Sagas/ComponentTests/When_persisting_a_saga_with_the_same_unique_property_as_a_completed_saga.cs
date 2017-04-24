namespace NServiceBus.Persistence.ComponentTests
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    public class When_persisting_a_saga_with_the_same_unique_property_as_a_completed_saga : SagaPersisterTests<SagaWithCorrelationProperty, SagaWithCorrelationPropertyData>
    {
        [Test]
        public async Task It_should_persist_successfully()
        {
            var correlationPropertyData = Guid.NewGuid().ToString();
            var saga1 = new SagaWithCorrelationPropertyData { CorrelatedProperty = correlationPropertyData, DateTimeProperty = DateTime.UtcNow };
            var saga2 = new SagaWithCorrelationPropertyData { CorrelatedProperty = correlationPropertyData, DateTimeProperty = DateTime.UtcNow };
            var saga3 = new SagaWithCorrelationPropertyData { CorrelatedProperty = correlationPropertyData, DateTimeProperty = DateTime.UtcNow };

            await SaveSaga(saga1);
            await GetByIdAndComplete(saga1.Id);

            await SaveSaga(saga2);
            await GetByIdAndComplete(saga2.Id);

            await SaveSaga(saga3);
            await GetByIdAndComplete(saga3.Id);
        }
    }
}