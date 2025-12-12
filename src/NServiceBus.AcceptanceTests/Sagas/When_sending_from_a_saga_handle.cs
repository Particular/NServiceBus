namespace NServiceBus.AcceptanceTests.Sagas;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_sending_from_a_saga_handle : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_match_different_saga()
    {
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

        public class TwoSaga1Saga1 : Saga<TwoSaga1Saga1Data>, IAmStartedByMessages<StartSaga1>, IHandleMessages<MessageSaga1WillHandle>
        {
            public Task Handle(StartSaga1 message, IMessageHandlerContext context)
            {
                Data.DataId = message.DataId;
                return context.SendLocal(new MessageSaga1WillHandle
                {
                    DataId = message.DataId
                });
            }

            public async Task Handle(MessageSaga1WillHandle message, IMessageHandlerContext context)
            {
                await context.SendLocal(new StartSaga2
                {
                    DataId = message.DataId
                });
                MarkAsComplete();
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TwoSaga1Saga1Data> mapper) =>
                mapper.MapSaga(s => s.DataId)
                    .ToMessage<MessageSaga1WillHandle>(m => m.DataId)
                    .ToMessage<StartSaga1>(m => m.DataId);
        }

        public class TwoSaga1Saga1Data : ContainSagaData
        {
            public virtual Guid DataId { get; set; }
        }

        public class TwoSaga1Saga2(Context testContext)
            : Saga<TwoSaga1Saga2.TwoSaga1Saga2Data>, IAmStartedByMessages<StartSaga2>
        {
            public Task Handle(StartSaga2 message, IMessageHandlerContext context)
            {
                Data.DataId = message.DataId;
                testContext.DidSaga2ReceiveMessage = true;
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TwoSaga1Saga2Data> mapper) =>
                mapper.MapSaga(s => s.DataId)
                    .ToMessage<StartSaga2>(m => m.DataId);

            public class TwoSaga1Saga2Data : ContainSagaData
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

    public class MessageSaga1WillHandle : IMessage
    {
        public Guid DataId { get; set; }
    }
}