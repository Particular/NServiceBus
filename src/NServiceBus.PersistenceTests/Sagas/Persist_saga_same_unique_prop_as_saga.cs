namespace NServiceBus.PersistenceTesting.Sagas
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;

    public class Persist_saga_same_unique_prop_as_saga : SagaPersisterTests
    {
        [Test]
        public async Task It_should_enforce_uniqueness()
        {
            var correlationPropertyData = Guid.NewGuid().ToString();
            var saga1 = new SagaWithCorrelationPropertyData
            {
                CorrelatedProperty = correlationPropertyData,
                DateTimeProperty = DateTime.UtcNow
            };
            var saga2 = new SagaWithCorrelationPropertyData
            {
                CorrelatedProperty = correlationPropertyData,
                DateTimeProperty = DateTime.UtcNow
            };

            var winningContextBag = configuration.GetContextBagForSagaStorage();
            using (var winningSession = configuration.CreateStorageSession())
            {
                await winningSession.Open(winningContextBag);

                await SaveSagaWithSession(saga1, winningSession, winningContextBag);
                await winningSession.CompleteAsync();
            }

            var losingContextBag = configuration.GetContextBagForSagaStorage();
            using (var losingSession = configuration.CreateStorageSession())
            {
                await losingSession.Open(losingContextBag);

                Assert.That(async () =>
                {
                    await SaveSagaWithSession(saga2, losingSession, losingContextBag);
                    await losingSession.CompleteAsync();
                }, Throws.InstanceOf<Exception>());
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

        public Persist_saga_same_unique_prop_as_saga(TestVariant param) : base(param)
        {
        }
    }


}
