namespace NServiceBus.PersistenceTesting.Sagas
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;

    public class When_persisting_a_saga_with_the_same_unique_property_as_another_saga : SagaPersisterTests
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
            using (var winningSession = await configuration.SynchronizedStorage.OpenSession(winningContextBag, CancellationToken.None))
            {
                await SaveSagaWithSession(saga1, winningSession, winningContextBag);
                await winningSession.CompleteAsync(CancellationToken.None);
            }

            var losingContextBag = configuration.GetContextBagForSagaStorage();
            using (var losingSession = await configuration.SynchronizedStorage.OpenSession(losingContextBag, CancellationToken.None))
            {
                Assert.That(async () =>
                {
                    await SaveSagaWithSession(saga2, losingSession, losingContextBag);
                    await losingSession.CompleteAsync(CancellationToken.None);
                }, Throws.InstanceOf<Exception>());
            }
        }

        public class SagaWithCorrelationProperty : Saga<SagaWithCorrelationPropertyData>, IAmStartedByMessages<SagaCorrelationPropertyStartingMessage>
        {
            public Task Handle(SagaCorrelationPropertyStartingMessage message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
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

        public When_persisting_a_saga_with_the_same_unique_property_as_another_saga(TestVariant param) : base(param)
        {
        }
    }


}
