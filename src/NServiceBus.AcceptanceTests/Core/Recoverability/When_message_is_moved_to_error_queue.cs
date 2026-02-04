namespace NServiceBus.AcceptanceTests.Core.Recoverability;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using EndpointTemplates;
using NUnit.Framework;

public class When_message_is_moved_to_error_queue : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_provide_clean_stack_trace()
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

        using (Assert.EnterMultipleScope())
        {
            var stackTrace = context.Headers["NServiceBus.ExceptionInfo.StackTrace"];
            Assert.That(stackTrace, Does.Not.Contain("MessageHandlerInvoker"));
            Assert.That(stackTrace, Does.Not.Contain("MessageHandlerFactory"));
        }
    }

    public class Context : ScenarioContext
    {
        public Dictionary<string, string> Headers { get; set; }
    }

    public class EndpointWithFailingHandler : EndpointConfigurationBuilder
    {
        public EndpointWithFailingHandler() =>
            EndpointSetup<DefaultServer>((b, context) =>
            {
                b.Recoverability().AddUnrecoverableException<SimulatedException>();
                b.SendFailedMessagesTo(Conventions.EndpointNamingConvention(typeof(ErrorSpy)));
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
                if (initiatingMessage.Id != testContext.TestRunId)
                {
                    return Task.CompletedTask;
                }

                testContext.Headers = context.MessageHeaders.Where(x => x.Key.StartsWith("NServiceBus.ExceptionInfo"))
                    .ToDictionary(x => x.Key, x => x.Value);
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class InitiatingMessage : IMessage
    {
        public Guid Id { get; set; }
    }
}