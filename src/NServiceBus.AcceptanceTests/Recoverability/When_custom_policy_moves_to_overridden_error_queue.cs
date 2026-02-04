namespace NServiceBus.AcceptanceTests.Recoverability;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using EndpointTemplates;
using NUnit.Framework;

public class When_custom_policy_moves_to_overridden_error_queue : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_move_to_defined_error_queue()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<EndpointWithFailingHandler>(b => b
                .DoNotFailOnErrorMessages()
                .When((session, ctx) => session.SendLocal(new InitiatingMessage
                {
                    Id = ctx.TestRunId
                }))
            )
            .WithEndpoint<ErrorSpy>()
            .Run();

        Assert.That(context.MessageMovedToErrorQueue, Is.True);
    }

    public class Context : ScenarioContext
    {
        public bool MessageMovedToErrorQueue { get; set; }
    }

    public class EndpointWithFailingHandler : EndpointConfigurationBuilder
    {
        public EndpointWithFailingHandler() =>
            EndpointSetup<DefaultServer>((config, context) =>
            {
                config.Recoverability().CustomPolicy((c, ec) =>
                    RecoverabilityAction.MoveToError(Conventions.EndpointNamingConvention(typeof(ErrorSpy))));

                config.SendFailedMessagesTo("error");
            });

        [Handler]
        public class InitiatingHandler : IHandleMessages<InitiatingMessage>
        {
            public Task Handle(InitiatingMessage initiatingMessage, IMessageHandlerContext context) => throw new SimulatedException();
        }
    }

    public class ErrorSpy : EndpointConfigurationBuilder
    {
        public ErrorSpy() => EndpointSetup<DefaultServer>();

        [Handler]
        public class InitiatingMessageHandler(Context testContext) : IHandleMessages<InitiatingMessage>
        {
            public Task Handle(InitiatingMessage initiatingMessage, IMessageHandlerContext context)
            {
                if (initiatingMessage.Id == testContext.TestRunId)
                {
                    testContext.MessageMovedToErrorQueue = true;
                    testContext.MarkAsCompleted();
                }

                return Task.CompletedTask;
            }
        }
    }

    public class InitiatingMessage : IMessage
    {
        public Guid Id { get; set; }
    }
}