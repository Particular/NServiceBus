namespace NServiceBus.AcceptanceTests.Core.Recoverability
{
    using System;
    using System.Collections.Generic;
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
                .Done(c => c.MessageMovedToErrorQueue)
                .Run();

            Assert.IsFalse(context.Headers.ContainsKey("NServiceBus.ExceptionInfo.ExceptionType"));
            Assert.AreEqual("this is a large message", context.Headers["NServiceBus.ExceptionInfo.Message"]);
            Assert.AreEqual("NotInventedHere", context.Headers["NServiceBus.ExceptionInfo.NotInventedHere"]);
        }

        class Context : ScenarioContext
        {
            public bool MessageMovedToErrorQueue { get; set; }
            public IReadOnlyDictionary<string, string> Headers { get; set; }
        }

        class EndpointWithFailingHandler : EndpointConfigurationBuilder
        {
            public EndpointWithFailingHandler()
            {
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
            }

            class InitiatingHandler : IHandleMessages<InitiatingMessage>
            {
                public Task Handle(InitiatingMessage initiatingMessage, IMessageHandlerContext context)
                {
                    throw new SimulatedException("THIS IS A LARGE MESSAGE");
                }
            }
        }

        class ErrorSpy : EndpointConfigurationBuilder
        {
            public ErrorSpy()
            {
                EndpointSetup<DefaultServer>();
            }

            class InitiatingMessageHandler : IHandleMessages<InitiatingMessage>
            {
                public InitiatingMessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(InitiatingMessage initiatingMessage, IMessageHandlerContext context)
                {
                    if (initiatingMessage.Id == testContext.TestRunId)
                    {
                        testContext.Headers = context.MessageHeaders.ToDictionary(x => x.Key, x => x.Value);
                        testContext.MessageMovedToErrorQueue = true;
                    }

                    return Task.FromResult(0);
                }

                Context testContext;
            }
        }

        public class InitiatingMessage : IMessage
        {
            public Guid Id { get; set; }
        }
    }
}