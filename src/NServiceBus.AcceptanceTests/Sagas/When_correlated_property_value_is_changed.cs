namespace NServiceBus.AcceptanceTests.Sagas;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Support;
using EndpointTemplates;
using NUnit.Framework;

[TestFixture]
public class When_correlated_property_value_is_changed : NServiceBusAcceptanceTest
{
    [Test]
    public void Should_throw()
    {
        var exception = Assert.ThrowsAsync<MessageFailedException>(async () =>
            await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(
                    b => b.When(session => session.SendLocal(new StartSaga
                    {
                        DataId = Guid.NewGuid()
                    })))
                .Run());

        Assert.That(exception.ScenarioContext.FailedMessages, Has.Count.EqualTo(1));
        Assert.That(
            exception.FailedMessage.Exception.Message,
            Does.Contain("Changing the value of correlated properties at runtime is currently not supported"));
    }

    public class Context : ScenarioContext;

    public class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint() => EndpointSetup<DefaultServer>();

        public class CorrIdChangedSaga(Context testContext) : Saga<CorrIdChangedSaga.CorrIdChangedSagaData>,
            IAmStartedByMessages<StartSaga>
        {
            public Task Handle(StartSaga message, IMessageHandlerContext context)
            {
                Data.DataId = Guid.NewGuid();
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<CorrIdChangedSagaData> mapper) =>
                mapper.MapSaga(s => s.DataId)
                    .ToMessage<StartSaga>(m => m.DataId);

            public class CorrIdChangedSagaData : ContainSagaData
            {
                public virtual Guid DataId { get; set; }
            }
        }
    }

    public class StartSaga : ICommand
    {
        public Guid DataId { get; set; }
    }
}