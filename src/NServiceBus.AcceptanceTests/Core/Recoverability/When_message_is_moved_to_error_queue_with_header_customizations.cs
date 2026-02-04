namespace NServiceBus.AcceptanceTests.Core.Recoverability;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using EndpointTemplates;
using NUnit.Framework;

public class When_message_is_moved_to_error_queue_with_header_customizations : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_apply_header_customizations()
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
            Assert.That(context.Headers.ContainsKey("NServiceBus.ExceptionInfo.ExceptionType"), Is.False);
            Assert.That(context.Headers["NServiceBus.ExceptionInfo.Message"], Is.EqualTo("this is a large message"));
            Assert.That(context.Headers["NServiceBus.ExceptionInfo.NotInventedHere"], Is.EqualTo("NotInventedHere"));
        }
    }

    public class Context : ScenarioContext
    {
        public Dictionary<string, string> Headers { get; set; }
    }

    public class EndpointWithFailingHandler : EndpointConfigurationBuilder
    {
        public EndpointWithFailingHandler() =>
            EndpointSetup<DefaultServer>((config, context) =>
            {
                config.Recoverability()
                    .Failed(failed => failed.HeaderCustomization(headers =>
                    {
                        headers.Remove("NServiceBus.ExceptionInfo.ExceptionType");
                        headers["NServiceBus.ExceptionInfo.Message"] = headers["NServiceBus.ExceptionInfo.Message"].ToLower();
                        headers["NServiceBus.ExceptionInfo.NotInventedHere"] = "NotInventedHere";
                    }));

                config.SendFailedMessagesTo(Conventions.EndpointNamingConvention(typeof(ErrorSpy)));
            });

        [Handler]
        public class InitiatingHandler : IHandleMessages<InitiatingMessage>
        {
            public Task Handle(InitiatingMessage initiatingMessage, IMessageHandlerContext context) => throw new SimulatedException("THIS IS A LARGE MESSAGE");
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
                    testContext.Headers = context.MessageHeaders.ToDictionary(x => x.Key, x => x.Value);
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