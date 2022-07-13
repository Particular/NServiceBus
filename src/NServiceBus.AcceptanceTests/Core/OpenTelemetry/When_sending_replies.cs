namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry
{
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_sending_replies : OpenTelemetryAcceptanceTest
    {
        [Test]
        public async Task Should_create_outgoing_message_span()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<TestEndpoint>(b => b
                    .When(s => s.SendLocal(new IncomingMessage())))
                .Done(c => c.OutgoingMessageReceived)
                .Run();

            var outgoingMessageActivities = NServicebusActivityListener.CompletedActivities.GetSendMessageActivities();
            Assert.AreEqual(2, outgoingMessageActivities.Count, "2 messages are being sent");
            var replyMessage = outgoingMessageActivities[1];

            Assert.AreEqual("reply", replyMessage.DisplayName);
            Assert.AreEqual(outgoingMessageActivities[0].RootId, replyMessage.RootId, "reply should belong to same trace as the triggering message");
            Assert.IsNotNull(replyMessage.ParentId, "reply should have ambient span");

            replyMessage.VerifyUniqueTags();
        }

        class Context : ScenarioContext
        {
            public string MessageConversationId { get; set; }
            public string OutgoingMessageId { get; set; }
            public bool OutgoingMessageReceived { get; set; }
        }

        class TestEndpoint : EndpointConfigurationBuilder
        {
            public TestEndpoint() => EndpointSetup<OpenTelemetryEnabledEndpoint>();

            class MessageHandler : IHandleMessages<IncomingMessage>,
                IHandleMessages<OutgoingReply>
            {
                Context testContext;

                public MessageHandler(Context testContext) => this.testContext = testContext;

                public Task Handle(IncomingMessage message, IMessageHandlerContext context)
                {
                    return context.Reply(new OutgoingReply());
                }

                public Task Handle(OutgoingReply message, IMessageHandlerContext context)
                {
                    testContext.MessageConversationId = context.MessageHeaders[Headers.ConversationId];
                    testContext.OutgoingMessageId = context.MessageId;
                    testContext.OutgoingMessageReceived = true;
                    return Task.CompletedTask;
                }
            }
        }

        public class IncomingMessage : IMessage
        {
        }

        public class OutgoingReply : IMessage
        {
        }
    }
}