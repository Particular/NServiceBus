namespace NServiceBus.PersistenceTesting.Sagas;

using System;
using System.Threading.Tasks;
using NUnit.Framework;

public class When_saga_not_found : SagaPersisterTests
{
    [Test]
    public async Task Should_return_null_when_loading_by_correlation_property()
    {
        var result = await GetByCorrelationProperty<SimpleSagaEntity>("UsedAsCorrelationId", "someValue");
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task Should_return_null_when_loading_by_id()
    {
        var result = await GetById<SimpleSagaEntity>(Guid.NewGuid());
        Assert.That(result, Is.Null);
    }

    public class SimpleSagaEntitySaga : Saga<SimpleSagaEntity>, IAmStartedByMessages<StartMessage>
    {
        public Task Handle(StartMessage message, IMessageHandlerContext context) => throw new NotImplementedException();

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SimpleSagaEntity> mapper) => mapper.MapSaga(s => s.UsedAsCorrelationId).ToMessage<StartMessage>(msg => msg.SomeId);
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