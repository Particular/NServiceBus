namespace NServiceBus.AcceptanceTests.Core.Pipeline
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
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
                        return s.Send(new MessageWithCoversationId(), options);
                    }))
                .Done(c => !string.IsNullOrEmpty(c.ReceivedConversationId))
                .Run();

            Assert.AreEqual(customConversationId, context.ReceivedConversationId);
        }

        [Test]
        public async Task Should_use_incoming_conversation_id_when_available()
        {
            var initialConversationId = Guid.NewGuid().ToString();

            var context = await Scenario.Define<Context>()
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
                .Run();

            Assert.AreEqual(initialConversationId, context.ReceivedConversationId);
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

            public class ConversationIdMessageHandler : IHandleMessages<MessageWithCoversationId>
            {
                Context testContext;

                public ConversationIdMessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(MessageWithCoversationId message, IMessageHandlerContext context)
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
                    .RouteToEndpoint(typeof(MessageWithCoversationId), typeof(ReceivingEndpoint)));
            }

            public class IntermediateMessageHandler : IHandleMessages<IntermediateMessage>
            {
                public Task Handle(IntermediateMessage message, IMessageHandlerContext context)
                {
                    var options = new SendOptions();
                    options.SetHeader(Headers.ConversationId, "intermediate message header");
                    return context.Send(new MessageWithCoversationId(), options);
                }
            }
        }

        public class MessageWithCoversationId : ICommand
        {
        }

        public class IntermediateMessage : ICommand
        {
        }
    }
}