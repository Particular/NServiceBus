namespace NServiceBus.Persistence.ComponentTests
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    public class When_updating_a_saga_with_the_same_unique_property_value : SagaPersisterTests<SagaWithCorrelationProperty, SagaWithCorrelationPropertyData>
    {
        [Test]
        public async Task It_should_persist_successfully()
        {
            var correlationPropertyData = Guid.NewGuid().ToString();
            var saga1 = new SagaWithCorrelationPropertyData
            {
                CorrelatedProperty = correlationPropertyData,
                DateTimeProperty = DateTime.UtcNow
            };

            await SaveSaga(saga1);

            var updatedValue = DateTime.UtcNow;
            var result = await GetByCorrelationPropertyAndUpdate(nameof(SagaWithCorrelationPropertyData.CorrelatedProperty), correlationPropertyData, saga => { saga.DateTimeProperty = updatedValue; });

            Assert.That(result, Is.Not.Null);
            Assert.That(result.DateTimeProperty, Is.EqualTo(updatedValue));
        }
    }
}