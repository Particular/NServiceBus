namespace NServiceBus.AcceptanceTests.Sagas;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using EndpointTemplates;
using NUnit.Framework;

// Repro for issue  https://github.com/NServiceBus/NServiceBus/issues/1277 to test the fix
// making sure that the saga correlation still works.
public class When_an_endpoint_replies_to_a_saga : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_correlate_all_saga_messages_properly()
    {
        var context = await Scenario.Define<Context>(c => { c.RunId = Guid.NewGuid(); })
            .WithEndpoint<EndpointThatHostsASaga>(b => b.When((session, ctx) => session.SendLocal(new StartSaga
            {
                RunId = ctx.RunId
            })))
            .WithEndpoint<EndpointThatRepliesToSagaMessage>()
            .Run();

        Assert.That(context.ResponseRunId, Is.EqualTo(context.RunId));
    }

    public class Context : ScenarioContext
    {
        public Guid RunId { get; set; }
        public Guid ResponseRunId { get; set; }
    }

    public class EndpointThatRepliesToSagaMessage : EndpointConfigurationBuilder
    {
        public EndpointThatRepliesToSagaMessage() => EndpointSetup<DefaultServer>();

        [Handler]
        public class DoSomethingHandler : IHandleMessages<DoSomething>
        {
            public Task Handle(DoSomething message, IMessageHandlerContext context) =>
                context.Reply(new DoSomethingResponse
                {
                    RunId = message.RunId
                });
        }
    }

    public class EndpointThatHostsASaga : EndpointConfigurationBuilder
    {
        public EndpointThatHostsASaga() =>
            EndpointSetup<DefaultServer>(c =>
            {
                c.ConfigureRouting().RouteToEndpoint(typeof(DoSomething), typeof(EndpointThatRepliesToSagaMessage));
            });

        [Saga]
        public class CorrelationTestSaga(Context testContext) : Saga<CorrelationTestSaga.CorrelationTestSagaData>,
            IAmStartedByMessages<StartSaga>,
            IHandleMessages<DoSomethingResponse>
        {
            public Task Handle(StartSaga message, IMessageHandlerContext context) =>
                context.Send(new DoSomething
                {
                    RunId = message.RunId
                });

            public Task Handle(DoSomethingResponse message, IMessageHandlerContext context)
            {
                testContext.ResponseRunId = message.RunId;
                MarkAsComplete();
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<CorrelationTestSagaData> mapper) =>
                mapper.MapSaga(s => s.RunId)
                    .ToMessage<StartSaga>(m => m.RunId)
                    .ToMessage<DoSomethingResponse>(m => m.RunId);

            public class CorrelationTestSagaData : ContainSagaData
            {
                public virtual Guid RunId { get; set; }
            }
        }
    }

    public class StartSaga : ICommand
    {
        public Guid RunId { get; set; }
    }

    public class DoSomething : ICommand
    {
        public Guid RunId { get; set; }
    }

    public class DoSomethingResponse : IMessage
    {
        public Guid RunId { get; set; }
    }
}