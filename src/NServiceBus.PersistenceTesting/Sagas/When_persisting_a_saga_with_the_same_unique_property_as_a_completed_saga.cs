namespace NServiceBus.PersistenceTesting.Sagas
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    public class When_persisting_a_saga_with_the_same_unique_property_as_a_completed_saga : SagaPersisterTests
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
            using (var completeSession = await configuration.SynchronizedStorage.OpenSession(context1))
            {
                var sagaData = await persister.Get<SagaWithCorrelationPropertyData>(nameof(saga1.CorrelatedProperty), correlationPropertyData, completeSession, context1);
                Assert.AreEqual(saga1.DataProperty, sagaData.DataProperty);

                await persister.Complete(sagaData, completeSession, context1);
                await completeSession.CompleteAsync();
            }
            
            Assert.IsNull(await GetById<SagaWithCorrelationPropertyData>(saga1.Id));

            await SaveSaga(saga2);
            var context2 = configuration.GetContextBagForSagaStorage();
            using (var completeSession = await configuration.SynchronizedStorage.OpenSession(context2))
            {
                var sagaData = await persister.Get<SagaWithCorrelationPropertyData>(nameof(saga2.CorrelatedProperty), correlationPropertyData, completeSession, context2);
                Assert.AreEqual(saga2.DataProperty, sagaData.DataProperty);

                await persister.Complete(sagaData, completeSession, context2);
                await completeSession.CompleteAsync();
            }
            Assert.IsNull(await GetById<SagaWithCorrelationPropertyData>(saga2.Id));

            Assert.AreNotEqual(saga1.Id, saga2.Id, "a new saga should be created each time");
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
    }
}