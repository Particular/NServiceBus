namespace NServiceBus.PersistenceTesting.Sagas
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;

    public class Persist_sagas_same_correlation_prop_val : SagaPersisterTests
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
            var saga2 = new AnotherSagaWithCorrelatedPropertyData
            {
                CorrelatedProperty = correlationPropertyData
            };

            var persister = configuration.SagaStorage;
            var savingContextBag = configuration.GetContextBagForSagaStorage();
            using (var session = await configuration.SynchronizedStorage.OpenSession(savingContextBag))
            {
                await SaveSagaWithSession(saga1, session, savingContextBag);
                await SaveSagaWithSession(saga2, session, savingContextBag);

                await session.CompleteAsync();
            }

            var readContextBag = configuration.GetContextBagForSagaStorage();
            using (var readSession = await configuration.SynchronizedStorage.OpenSession(readContextBag))
            {
                var saga1Result = await persister.Get<SagaWithCorrelationPropertyData>(nameof(SagaWithCorrelationPropertyData.CorrelatedProperty), saga1.CorrelatedProperty, readSession, readContextBag);

                var saga2Result = await persister.Get<AnotherSagaWithCorrelatedPropertyData>(nameof(AnotherSagaWithCorrelatedPropertyData.CorrelatedProperty), saga2.CorrelatedProperty, readSession, readContextBag);

                Assert.AreEqual(saga1.CorrelatedProperty, saga1Result.CorrelatedProperty);
                Assert.AreEqual(saga2.CorrelatedProperty, saga2Result.CorrelatedProperty);
            }
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

            public DateTime DateTimeProperty { get; set; }
        }

        public class SagaCorrelationPropertyStartingMessage
        {
            public string CorrelatedProperty { get; set; }
        }

        class AnotherSagaWithCorrelatedProperty : Saga<AnotherSagaWithCorrelatedPropertyData>, IAmStartedByMessages<TwoUniquePropertyStartingMessage>
        {
            public Task Handle(TwoUniquePropertyStartingMessage message, IMessageHandlerContext context)
            {
                throw new NotImplementedException();
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<AnotherSagaWithCorrelatedPropertyData> mapper)
            {
                mapper.ConfigureMapping<TwoUniquePropertyStartingMessage>(m => m.CorrelatedProperty).ToSaga(s => s.CorrelatedProperty);
            }
        }

        class TwoUniquePropertyStartingMessage
        {
            public string CorrelatedProperty { get; set; }
        }

        public class AnotherSagaWithCorrelatedPropertyData : ContainSagaData
        {
            public string CorrelatedProperty { get; set; }
        }

        public Persist_sagas_same_correlation_prop_val(TestVariant param) : base(param)
        {
        }
    }
}
