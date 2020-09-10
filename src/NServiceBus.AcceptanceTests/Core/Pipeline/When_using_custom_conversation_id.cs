namespace NServiceBus.AcceptanceTests.Core.Pipeline
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_using_custom_conversation_id : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_apply_custom_conversation_id_when_no_incoming_message()
        {
            var customConversationId = Guid.NewGuid().ToString();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<ReceivingEndpoint>(e => e
                    .When(s =>
                    {
                        var options = new SendOptions();
                        options.RouteToThisEndpoint();
                        options.SetHeader(Headers.ConversationId, customConversationId);
                        return s.Send(new MessageWithConversationId(), options);
                    }))
                .Done(c => !string.IsNullOrEmpty(c.ReceivedConversationId))
                .Run();

            Assert.AreEqual(customConversationId, context.ReceivedConversationId);
        }

        class Context : ScenarioContext
        {
            public string ReceivedConversationId { get; set; }
        }

        class ReceivingEndpoint : EndpointConfigurationBuilder
        {
            public ReceivingEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class ConversationIdMessageHandler : IHandleMessages<MessageWithConversationId>
            {
                Context testContext;

                public ConversationIdMessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(MessageWithConversationId message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    testContext.ReceivedConversationId = context.MessageHeaders[Headers.ConversationId];
                    return Task.FromResult(0);
                }
            }
        }

        public class MessageWithConversationId : ICommand
        {
        }
    }
}