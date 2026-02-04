namespace NServiceBus.AcceptanceTests.Sagas;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_using_contain_saga_data : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_handle_timeouts_properly()
    {
        Requires.DelayedDelivery();

        var context = await Scenario.Define<Context>()
            .WithEndpoint<EndpointThatHostsASaga>(
                b => b.When(session => session.SendLocal(new StartSaga
                {
                    DataId = Guid.NewGuid()
                })))
            .Run();

        Assert.That(context.TimeoutReceived, Is.True);
    }

    public class Context : ScenarioContext
    {
        public bool TimeoutReceived { get; set; }
    }

    public class EndpointThatHostsASaga : EndpointConfigurationBuilder
    {
        public EndpointThatHostsASaga() => EndpointSetup<DefaultServer>();

        [Saga]
        public class MySaga(Context testContext) : Saga<MySaga.MySagaData>,
            IAmStartedByMessages<StartSaga>,
            IHandleTimeouts<MySaga.TimeHasPassed>
        {
            public Task Handle(StartSaga message, IMessageHandlerContext context)
            {
                Data.DataId = message.DataId;

                return RequestTimeout(context, TimeSpan.FromMilliseconds(1), new TimeHasPassed());
            }

            public Task Timeout(TimeHasPassed state, IMessageHandlerContext context)
            {
                MarkAsComplete();
                testContext.TimeoutReceived = true;
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MySagaData> mapper) =>
                mapper.MapSaga(s => s.DataId)
                    .ToMessage<StartSaga>(m => m.DataId);

            public class MySagaData : ContainSagaData
            {
                public virtual Guid DataId { get; set; }
            }

            public class TimeHasPassed;
        }
    }

    public class StartSaga : IMessage
    {
        public Guid DataId { get; set; }
    }
}