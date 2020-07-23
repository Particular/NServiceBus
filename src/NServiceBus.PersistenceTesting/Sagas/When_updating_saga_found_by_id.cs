namespace NServiceBus.PersistenceTesting.Sagas
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixtureSource(typeof(SagaTestVariantSource), "Variants")]
    public class When_updating_saga_found_by_id : SagaPersisterTests
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
            var persister = configuration.SagaStorage;
            using (var completeSession = await configuration.SynchronizedStorage.OpenSession(context))
            {
                var sagaData = await persister.Get<SagaWithCorrelationPropertyData>(saga1.Id, completeSession, context);

                sagaData.SomeProperty = updatedValue;

                await persister.Update(sagaData, completeSession, context);
                await completeSession.CompleteAsync();
            }

            var updatedSagaData = await GetById<SagaWithCorrelationPropertyData>(saga1.Id);

            Assert.That(updatedSagaData, Is.Not.Null);
            Assert.That(updatedSagaData.SomeProperty, Is.EqualTo(updatedValue));
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

            public string SomeProperty { get; set; }
        }

        public class SagaCorrelationPropertyStartingMessage
        {
            public string CorrelatedProperty { get; set; }
        }

        public When_updating_saga_found_by_id(TestVariant param) : base(param)
        {
        }
    }
}