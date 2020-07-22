namespace NServiceBus.PersistenceTesting.Sagas
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    public class When_saga_not_found : SagaPersisterTests<When_saga_not_found.SimpleSagaEntity>
    {
        [Test]
        public async Task Should_throw_when_using_finding_saga_with_correlation_property()
        {
            var result = await GetByCorrelationProperty("UsedAsCorrelationId", "someValue");
            Assert.IsNull(result);
        }

        [Test]
        public async Task Should_return_default_when_using_finding_saga_with_id()
        {
            var result = await GetById<SimpleSagaEntity>(Guid.NewGuid());
            Assert.IsNull(result);
        }

        public class AnotherSimpleSagaEntity : ContainSagaData
        {
        }

        public class SimpleSagaEntitySaga : Saga<SimpleSagaEntity>, IAmStartedByMessages<StartMessage>
        {
            public Task Handle(StartMessage message, IMessageHandlerContext context)
            {
                throw new NotImplementedException();
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SimpleSagaEntity> mapper)
            {
                mapper.ConfigureMapping<StartMessage>(msg => msg.SomeId).ToSaga(saga => saga.UsedAsCorrelationId);
            }
        }

        public class SimpleSagaEntity : ContainSagaData
        {
            public string UsedAsCorrelationId { get; set; }
        }

        public class StartMessage
        {
            public string SomeId { get; set; }
        }
    }
}