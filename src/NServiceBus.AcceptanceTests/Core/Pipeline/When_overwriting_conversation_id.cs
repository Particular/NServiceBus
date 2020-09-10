namespace NServiceBus.AcceptanceTests.Core.Pipeline
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Support;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_overwriting_conversation_id : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_throw_when_incoming_conversation_id_available()
        {
            var initialConversationId = Guid.NewGuid().ToString();

            var exception = Assert.ThrowsAsync<MessageFailedException>(() => Scenario.Define<Context>()
                .WithEndpoint<ReceivingEndpoint>(e => e
                    .When(s =>
                    {
                        var options = new SendOptions();
                        options.RouteToThisEndpoint();
                        options.SetHeader(Headers.ConversationId, initialConversationId);
                        return s.Send(new IntermediateMessage(), options);
                    }))
                .Done(c => c.ReceivedMessage)
                .Run());

            StringAssert.Contains($"Cannot set the {Headers.ConversationId} header to 'intermediate message header' as it cannot override the incoming header value ('{initialConversationId}').", exception.InnerException.Message);
            Assert.IsFalse(((Context)exception.ScenarioContext).SentOutgoingMessage, "because send should fail");
        }

        class Context : ScenarioContext
        {
            public bool ReceivedMessage { get; set; }
            public bool SentOutgoingMessage { get; set; }
        }

        class ReceivingEndpoint : EndpointConfigurationBuilder
        {
            public ReceivingEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class IntermediateMessageHandler : IHandleMessages<IntermediateMessage>
            {
                Context testContext;

                public IntermediateMessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public async Task Handle(IntermediateMessage message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    testContext.ReceivedMessage = true;

                    var options = new SendOptions();
                    options.RouteToThisEndpoint();
                    options.SetHeader(Headers.ConversationId, "intermediate message header");
                    await context.Send(new MessageWithConversationId(), options);

                    testContext.SentOutgoingMessage = true;
                }
            }
        }

        public class MessageWithConversationId : ICommand
        {
        }

        public class IntermediateMessage : ICommand
        {
        }
    }
}