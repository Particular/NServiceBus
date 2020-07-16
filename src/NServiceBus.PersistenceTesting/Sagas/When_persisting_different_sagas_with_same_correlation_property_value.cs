namespace NServiceBus.PersistenceTesting.Sagas
{
    using NUnit.Framework;
    using System;
    using System.Threading.Tasks;

    [TestFixture]
    public class When_persisting_different_sagas_with_same_correlation_property_value : SagaPersisterTests
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
                var correlationPropertySaga1 = SetActiveSagaInstanceForSave(savingContextBag, new SagaWithCorrelationProperty(), saga1);
                await persister.Save(saga1, correlationPropertySaga1, session, savingContextBag);

                var correlationPropertySaga2 = SetActiveSagaInstanceForSave(savingContextBag, new AnotherSagaWithCorrelatedProperty(), saga2);
                await persister.Save(saga2, correlationPropertySaga2, session, savingContextBag);

                await session.CompleteAsync();
            }

            var readContextBag = configuration.GetContextBagForSagaStorage();
            using (var readSession = await configuration.SynchronizedStorage.OpenSession(readContextBag))
            {
                SetActiveSagaInstanceForGet<SagaWithCorrelationProperty, SagaWithCorrelationPropertyData>(readContextBag, saga1);
                var saga1Result = await persister.Get<SagaWithCorrelationPropertyData>(nameof(SagaWithCorrelationPropertyData.CorrelatedProperty), saga1.CorrelatedProperty, readSession, readContextBag);

                SetActiveSagaInstanceForGet<AnotherSagaWithCorrelatedProperty, AnotherSagaWithCorrelatedPropertyData>(readContextBag, saga2);
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
    }
}
