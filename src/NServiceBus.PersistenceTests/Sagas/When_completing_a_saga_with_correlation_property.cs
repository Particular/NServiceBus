﻿namespace NServiceBus.PersistenceTesting.Sagas;

using System;
using System.Threading.Tasks;
using NUnit.Framework;

public class When_completing_a_saga_with_correlation_property : SagaPersisterTests
{
    [Test]
    public async Task Should_delete_the_saga()
    {
        var correlationPropertyData = Guid.NewGuid().ToString();
        var saga = new SagaWithCorrelationPropertyData { CorrelatedProperty = correlationPropertyData, DateTimeProperty = DateTime.UtcNow };
        await SaveSaga(saga);

        const string correlatedPropertyName = nameof(SagaWithCorrelationPropertyData.CorrelatedProperty);
        var context = configuration.GetContextBagForSagaStorage();
        await using (var completeSession = configuration.CreateStorageSession())
        {
            await completeSession.Open(context);

            var sagaData = await configuration.SagaStorage.Get<SagaWithCorrelationPropertyData>(correlatedPropertyName, correlationPropertyData, completeSession, context);

            await configuration.SagaStorage.Complete(sagaData, completeSession, context);
            await completeSession.CompleteAsync();
        }

        var completedSaga = await GetByCorrelationProperty<SagaWithCorrelationPropertyData>(correlatedPropertyName, correlationPropertyData);
        Assert.That(completedSaga, Is.Null);
    }

    public class SagaWithCorrelationPropertyData : ContainSagaData
    {
        public string CorrelatedProperty { get; set; }

        public DateTime DateTimeProperty { get; set; }
    }

    public class SagaWithCorrelationProperty : Saga<SagaWithCorrelationPropertyData>, IAmStartedByMessages<SagaCorrelationPropertyStartingMessage>
    {
        public Task Handle(SagaCorrelationPropertyStartingMessage message, IMessageHandlerContext context)
        {
            throw new NotImplementedException();
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaWithCorrelationPropertyData> mapper)
        {
            mapper.ConfigureMapping<SagaCorrelationPropertyStartingMessage>(m => m.CorrelatedProperty).ToSaga(s => s.CorrelatedProperty);
        }
    }

    public class SagaCorrelationPropertyStartingMessage
    {
        public string CorrelatedProperty { get; set; }
    }

    public When_completing_a_saga_with_correlation_property(TestVariant param) : base(param)
    {
    }
}