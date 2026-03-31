namespace NServiceBus.AcceptanceTests.Sagas;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_sending_from_a_saga_timeout : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_match_different_saga()
    {
        Requires.DelayedDelivery();

        var context = await Scenario.Define<Context>()
            .WithEndpoint<Endpoint>(b => b.When(session => session.SendLocal(new StartSaga1
            {
                DataId = Guid.NewGuid()
            })))
            .Run();

        Assert.That(context.DidSaga2ReceiveMessage, Is.True);
    }

    public class Context : ScenarioContext
    {
        public bool DidSaga2ReceiveMessage { get; set; }
    }

    public class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint() => EndpointSetup<DefaultServer>();

        [Saga]
        public class SendFromTimeoutSaga1 : Saga<SendFromTimeoutSaga1.SendFromTimeoutSaga1Data>,
            IAmStartedByMessages<StartSaga1>,
            IHandleTimeouts<Saga1Timeout>
        {
            public Task Handle(StartSaga1 message, IMessageHandlerContext context)
            {
                Data.DataId = message.DataId;
                return RequestTimeout(context, TimeSpan.FromMilliseconds(1), new Saga1Timeout());
            }

            public async Task Timeout(Saga1Timeout state, IMessageHandlerContext context)
            {
                await context.SendLocal(new StartSaga2
                {
                    DataId = Data.DataId
                });
                MarkAsComplete();
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SendFromTimeoutSaga1Data> mapper) =>
                mapper.MapSaga(s => s.DataId)
                    .ToMessage<StartSaga1>(m => m.DataId);

            public class SendFromTimeoutSaga1Data : ContainSagaData
            {
                public virtual Guid DataId { get; set; }
            }
        }

        [Saga]
        public class SendFromTimeoutSaga2(Context testContext) : Saga<SendFromTimeoutSaga2.SendFromTimeoutSaga2Data>,
            IAmStartedByMessages<StartSaga2>
        {
            public Task Handle(StartSaga2 message, IMessageHandlerContext context)
            {
                Data.DataId = message.DataId;
                testContext.DidSaga2ReceiveMessage = true;
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SendFromTimeoutSaga2Data> mapper) =>
                mapper.MapSaga(s => s.DataId)
                    .ToMessage<StartSaga2>(m => m.DataId);

            public class SendFromTimeoutSaga2Data : ContainSagaData
            {
                public virtual Guid DataId { get; set; }
            }
        }
    }

    public class StartSaga1 : ICommand
    {
        public Guid DataId { get; set; }
    }

    public class StartSaga2 : ICommand
    {
        public Guid DataId { get; set; }
    }

    public class Saga1Timeout : IMessage;
}