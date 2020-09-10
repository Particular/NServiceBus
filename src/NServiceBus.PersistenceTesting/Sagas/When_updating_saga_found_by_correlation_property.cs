namespace NServiceBus.PersistenceTesting.Sagas
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;

    public class When_updating_saga_found_by_correlation_property : SagaPersisterTests
    {
        [Test]
        public async Task It_should_persist_successfully()
        {
            var correlationPropertyData = Guid.NewGuid().ToString();
            var saga1 = new SagaWithCorrelationPropertyData
            {
                CorrelatedProperty = correlationPropertyData,
                SomeProperty = "foo"
            };

            await SaveSaga(saga1);

            var updatedValue = "bar";
            var context = configuration.GetContextBagForSagaStorage();
            var correlatedPropertyName = nameof(SagaWithCorrelationPropertyData.CorrelatedProperty);
            var persister = configuration.SagaStorage;
            using (var completeSession = await configuration.SynchronizedStorage.OpenSession(context, CancellationToken.None))
            {
                var sagaData = await persister.Get<SagaWithCorrelationPropertyData>(correlatedPropertyName, correlationPropertyData, completeSession, context, CancellationToken.None);

                sagaData.SomeProperty = updatedValue;

                await persister.Update(sagaData, completeSession, context, CancellationToken.None);
                await completeSession.CompleteAsync(CancellationToken.None);
            }

            var updatedSagaData = await GetByCorrelationProperty<SagaWithCorrelationPropertyData>(correlatedPropertyName, correlationPropertyData);

            Assert.That(updatedSagaData, Is.Not.Null);
            Assert.That(updatedSagaData.SomeProperty, Is.EqualTo(updatedValue));
        }

        public class SagaWithCorrelationProperty : Saga<SagaWithCorrelationPropertyData>, IAmStartedByMessages<SagaCorrelationPropertyStartingMessage>
        {
            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaWithCorrelationPropertyData> mapper)
            {
                mapper.ConfigureMapping<SagaCorrelationPropertyStartingMessage>(m => m.CorrelatedProperty).ToSaga(s => s.CorrelatedProperty);
            }

            public Task Handle(SagaCorrelationPropertyStartingMessage message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
        }

        public class SagaWithCorrelationPropertyData : ContainSagaData
        {
            public string CorrelatedProperty { get; set; }

            public string SomeProperty { get; set; }
        }

        public class SagaCorrelationPropertyStartingMessage
        {
            public string CorrelatedProperty { get; set; }
        }

        public When_updating_saga_found_by_correlation_property(TestVariant param) : base(param)
        {
        }
    }
}