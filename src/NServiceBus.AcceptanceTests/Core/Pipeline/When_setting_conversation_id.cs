namespace NServiceBus.AcceptanceTests.Core.Pipeline
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using AcceptanceTesting.Support;
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

        [Test]
        public void Should_throw_when_incoming_conversation_id_available()
        {
            var initialConversationId = Guid.NewGuid().ToString();

            var exception = Assert.ThrowsAsync<MessageFailedException>(() => Scenario.Define<Context>()
                .WithEndpoint<IntermediateEndpoint>(e => e
                    .When(s =>
                    {
                        var options = new SendOptions();
                        options.RouteToThisEndpoint();
                        options.SetHeader(Headers.ConversationId, initialConversationId);
                        return s.Send(new IntermediateMessage(), options);
                    }))
                .WithEndpoint<ReceivingEndpoint>()
                .Done(c => !string.IsNullOrEmpty(c.ReceivedConversationId))
                .Run());

            StringAssert.Contains($"Cannot set the {Headers.ConversationId} header to 'intermediate message header' as it cannot override the incoming header value ('{initialConversationId}').", exception.InnerException.Message);
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

                public Task Handle(MessageWithConversationId message, IMessageHandlerContext context)
                {
                    testContext.ReceivedConversationId = context.MessageHeaders[Headers.ConversationId];
                    return Task.FromResult(0);
                }
            }
        }

        class IntermediateEndpoint : EndpointConfigurationBuilder
        {
            public IntermediateEndpoint()
            {
                EndpointSetup<DefaultServer>(e => e.ConfigureTransport().Routing()
                    .RouteToEndpoint(typeof(MessageWithConversationId), typeof(ReceivingEndpoint)));
            }

            public class IntermediateMessageHandler : IHandleMessages<IntermediateMessage>
            {
                public Task Handle(IntermediateMessage message, IMessageHandlerContext context)
                {
                    var options = new SendOptions();
                    options.SetHeader(Headers.ConversationId, "intermediate message header");
                    return context.Send(new MessageWithConversationId(), options);
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