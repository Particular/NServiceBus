namespace NServiceBus.AcceptanceTests.Core.Recoverability
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using MessageMutator;
    using NUnit.Framework;

    public class When_message_is_moved_to_error_queue_with_header_customizations : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_apply_header_customizations()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithFailingHandler>(b => b
                    .DoNotFailOnErrorMessages()
                    .When((session, ctx) => session.SendLocal(new InitiatingMessage { Id = ctx.TestRunId }))
                )
                .WithEndpoint<ErrorSpy>()
                .Done(c => c.MessageMovedToErrorQueue)
                .Run();

            Assert.IsFalse(context.Headers.ContainsKey("NServiceBus.ExceptionInfo.ExceptionType"));
            Assert.AreEqual("this is a large message", context.Headers["NServiceBus.ExceptionInfo.Message"]);
            Assert.AreEqual("NotInventedHere", context.Headers["NServiceBus.ExceptionInfo.NotInventedHere"]);
            Assert.True(context.Headers.ContainsKey("mutator-header-present"), "Header set by outgoing message mutator should be available to header customizations");
            Assert.AreEqual("set-by-mutator", context.Headers["header-set-by-outgoing-mutator"]);
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
                EndpointSetup<DefaultServer>((config, _) =>
                {
                    config.Recoverability()
                        .Failed(failed => failed.HeaderCustomization(headers =>
                        {
                            headers.Remove("NServiceBus.ExceptionInfo.ExceptionType");
                            headers["NServiceBus.ExceptionInfo.Message"] = headers["NServiceBus.ExceptionInfo.Message"].ToLower();
                            headers["NServiceBus.ExceptionInfo.NotInventedHere"] = "NotInventedHere";
                            if (headers.ContainsKey("header-set-by-outgoing-mutator"))
                            {
                                headers["mutator-header-present"] = "true";
                            }
                        }));

                    config.RegisterMessageMutator(new OutgoingMessageMutator());
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

            class OutgoingMessageMutator : IMutateOutgoingTransportMessages
            {
                public Task MutateOutgoing(MutateOutgoingTransportMessageContext context)
                {
                    context.OutgoingHeaders["header-set-by-outgoing-mutator"] = "set-by-mutator";

                    return Task.FromResult(0);
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

                    return Task.CompletedTask;
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