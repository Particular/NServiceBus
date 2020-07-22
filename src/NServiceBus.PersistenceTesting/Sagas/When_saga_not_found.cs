namespace NServiceBus.PersistenceTesting.Sagas
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixtureSource(typeof(SagaTestVariantSource), "Variants")]
    public class When_saga_not_found : SagaPersisterTests
    {
        [Test]
        public async Task Should_return_null_when_loading_by_correlation_property()
        {
            var result = await GetByCorrelationProperty<SimpleSagaEntity>("UsedAsCorrelationId", "someValue");
            Assert.IsNull(result);
        }

        [Test]
        public async Task Should_return_null_when_loading_by_id()
        {
            var result = await GetById<SimpleSagaEntity>(Guid.NewGuid());
            Assert.IsNull(result);
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

        public When_saga_not_found(TestVariant param) : base(param)
        {
        }
    }
}