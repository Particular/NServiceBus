namespace NServiceBus.PersistenceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    public class When_updating_a_saga_found_using_correlation_property : SagaPersisterTests<When_updating_a_saga_found_using_correlation_property.SagaWithCorrelationProperty, When_updating_a_saga_found_using_correlation_property.SagaWithCorrelationPropertyData>
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
            var result = await GetByCorrelationPropertyAndUpdate(nameof(SagaWithCorrelationPropertyData.CorrelatedProperty), correlationPropertyData, saga => { saga.SomeProperty = updatedValue; });

            Assert.That(result, Is.Not.Null);
            Assert.That(result.SomeProperty, Is.EqualTo(updatedValue));
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

        public class SagaWithCorrelationPropertyData : ContainSagaData
        {
            public string CorrelatedProperty { get; set; }

            public string SomeProperty { get; set; }
        }

        public class SagaCorrelationPropertyStartingMessage
        {
            public string CorrelatedProperty { get; set; }
        }
    }
}