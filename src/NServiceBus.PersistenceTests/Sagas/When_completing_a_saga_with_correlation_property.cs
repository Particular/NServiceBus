﻿namespace NServiceBus.PersistenceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    public class When_completing_a_saga_with_correlation_property : SagaPersisterTests<SagaWithCorrelationProperty, SagaWithCorrelationPropertyData>
    {
        [Test]
        public async Task Should_delete_the_saga()
        {
            var correlationPropertyData = Guid.NewGuid().ToString();

            var saga = new SagaWithCorrelationPropertyData {CorrelatedProperty = correlationPropertyData, DateTimeProperty = DateTime.UtcNow};

            await SaveSaga(saga);

            const string correlatedPropertyName = nameof(SagaWithCorrelationPropertyData.CorrelatedProperty);

            var sagaData = await GetByCorrelationPropertyAndComplete(correlatedPropertyName, correlationPropertyData);
            var completedSaga = await GetByCorrelationProperty(correlatedPropertyName, correlationPropertyData);

            Assert.NotNull(sagaData);
            Assert.Null(completedSaga);
        }
    }
}