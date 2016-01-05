namespace NServiceBus.AcceptanceTests.Causation
{
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_a_message_is_audited : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_flow_causation_headers()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<CausationEndpoint>(b => b.When(bus => bus.SendLocal(new FirstMessage())))
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
                EndpointSetup<DefaultServer>().AuditTo<AuditSpyEndpoint>();
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
                public Context TestContext { get; set; }

                public Task Handle(FirstMessage message, IMessageHandlerContext context)
                {
                    TestContext.OriginRelatedTo = context.MessageId;
                    TestContext.OriginConversationId = context.MessageHeaders.ContainsKey(Headers.ConversationId) ? context.MessageHeaders[Headers.ConversationId] : null;

                    context.SendLocal(new MessageToBeAudited());
                    return Task.FromResult(0);
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
                public Context TestContext { get; set; }

                public Task Handle(MessageToBeAudited message, IMessageHandlerContext context)
                {
                    TestContext.RelatedTo = context.MessageHeaders.ContainsKey(Headers.RelatedTo) ? context.MessageHeaders[Headers.RelatedTo] : null;
                    TestContext.ConversationId = context.MessageHeaders.ContainsKey(Headers.ConversationId) ? context.MessageHeaders[Headers.ConversationId] : null;
                    TestContext.Done = true;

                    return Task.FromResult(0);
                }
            }

            public class FirstMessageHandler : IHandleMessages<FirstMessage>
            {
                public Context TestContext { get; set; }

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