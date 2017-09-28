namespace NServiceBus.AcceptanceTests.Pipeline
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_a_message_is_audited : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_flow_causation_headers()
        {
            var context = await new Scenario<Context>()
                .WithEndpoint<CausationEndpoint>(b => b.When(session => session.SendLocal(new FirstMessage())))
                .WithEndpoint<AuditSpyEndpoint>()
                .Done(c => c.Done)
                .Run();

            Assert.AreEqual(context.OriginRelatedTo, context.RelatedTo, "The RelatedTo header in audit message should be be equal to RelatedTo header in origin.");
            Assert.AreEqual(context.OriginConversationId, context.ConversationId, "The ConversationId header in audit message should be be equal to ConversationId header in origin.");
        }

        public class Context : ScenarioContext
        {
            public bool Done { get; set; }
            public string RelatedTo { get; set; }
            public string ConversationId { get; set; }
            public string OriginRelatedTo { get; set; }
            public string OriginConversationId { get; set; }
        }

        public class CausationEndpoint : EndpointConfigurationBuilder
        {
            public CausationEndpoint()
            {
                EndpointSetup<DefaultServer>(c => c.AuditProcessedMessagesTo<AuditSpyEndpoint>());
            }

            public Context Context { get; set; }

            public class MessageSentInsideHandlersHandler : IHandleMessages<MessageToBeAudited>
            {
                public Task Handle(MessageToBeAudited message, IMessageHandlerContext context)
                {
                    return Task.FromResult(0);
                }
            }

            public class FirstMessageHandler : IHandleMessages<FirstMessage>
            {
                public Task Handle(FirstMessage message, IMessageHandlerContext context)
                {
                    var testContext = Scenario<Context>.CurrentContext.Value;
                    testContext.OriginRelatedTo = context.MessageId;
                    testContext.OriginConversationId = context.MessageHeaders.ContainsKey(Headers.ConversationId) ? context.MessageHeaders[Headers.ConversationId] : null;
                    
                    return context.SendLocal(new MessageToBeAudited());
                }
            }
        }

        class AuditSpyEndpoint : EndpointConfigurationBuilder
        {
            public AuditSpyEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class MessageToBeAuditedHandler : IHandleMessages<MessageToBeAudited>
            {
                public Task Handle(MessageToBeAudited message, IMessageHandlerContext context)
                {
                    var testContext = Scenario<Context>.CurrentContext.Value;

                    testContext.RelatedTo = context.MessageHeaders.ContainsKey(Headers.RelatedTo) ? context.MessageHeaders[Headers.RelatedTo] : null;
                    testContext.ConversationId = context.MessageHeaders.ContainsKey(Headers.ConversationId) ? context.MessageHeaders[Headers.ConversationId] : null;
                    testContext.Done = true;

                    return Task.FromResult(0);
                }
            }

            public class FirstMessageHandler : IHandleMessages<FirstMessage>
            {
                public Task Handle(FirstMessage message, IMessageHandlerContext context)
                {
                    return Task.FromResult(0);
                }
            }
        }

        public class FirstMessage : IMessage
        {
        }

        public class MessageToBeAudited : IMessage
        {
        }
    }
}