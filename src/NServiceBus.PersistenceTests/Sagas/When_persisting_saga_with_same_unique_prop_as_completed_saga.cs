namespace NServiceBus.PersistenceTesting.Sagas;

using System;
using System.Threading.Tasks;
using NUnit.Framework;

public class When_persisting_saga_with_same_unique_prop_as_completed_saga : SagaPersisterTests
{
    [Test]
    public async Task It_should_persist_successfully()
    {
        var correlationPropertyData = Guid.NewGuid().ToString();
        var saga1 = new SagaWithCorrelationPropertyData { CorrelatedProperty = correlationPropertyData, DataProperty = "saga1" };
        var saga2 = new SagaWithCorrelationPropertyData { CorrelatedProperty = correlationPropertyData, DataProperty = "saga2" };

        var persister = configuration.SagaStorage;

        await SaveSaga(saga1);
        var context1 = configuration.GetContextBagForSagaStorage();
        await using (var completeSession = configuration.CreateStorageSession())
        {
            await completeSession.Open(context1);

            var sagaData = await persister.Get<SagaWithCorrelationPropertyData>(nameof(saga1.CorrelatedProperty), correlationPropertyData, completeSession, context1);
            Assert.That(sagaData.DataProperty, Is.EqualTo(saga1.DataProperty));

            await persister.Complete(sagaData, completeSession, context1);
            await completeSession.CompleteAsync();
        }

        Assert.That(await GetById<SagaWithCorrelationPropertyData>(saga1.Id), Is.Null);

        await SaveSaga(saga2);
        var context2 = configuration.GetContextBagForSagaStorage();
        await using (var completeSession = configuration.CreateStorageSession())
        {
            await completeSession.Open(context2);

            var sagaData = await persister.Get<SagaWithCorrelationPropertyData>(nameof(saga2.CorrelatedProperty), correlationPropertyData, completeSession, context2);
            Assert.That(sagaData.DataProperty, Is.EqualTo(saga2.DataProperty));

            await persister.Complete(sagaData, completeSession, context2);
            await completeSession.CompleteAsync();
        }

        Assert.That(await GetById<SagaWithCorrelationPropertyData>(saga2.Id), Is.Null);
    }

    public class SagaWithCorrelationProperty : Saga<SagaWithCorrelationPropertyData>, IAmStartedByMessages<SagaCorrelationPropertyStartingMessage>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaWithCorrelationPropertyData> mapper)
        {
            mapper.ConfigureMapping<SagaCorrelationPropertyStartingMessage>(m => m.CorrelatedProperty).ToSaga(s => s.CorrelatedProperty);
        }

        public Task Handle(SagaCorrelationPropertyStartingMessage message, IMessageHandlerContext context)
        {
            throw new NotImplementedException();
        }
    }

    public class SagaWithCorrelationPropertyData : ContainSagaData
    {
        public string CorrelatedProperty { get; set; }

        public string DataProperty { get; set; }
    }

    public class SagaCorrelationPropertyStartingMessage
    {
        public string CorrelatedProperty { get; set; }
    }

    public When_persisting_saga_with_same_unique_prop_as_completed_saga(TestVariant param) : base(param)
    {
    }
}