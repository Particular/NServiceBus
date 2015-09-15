namespace NServiceBus.AcceptanceTests.Causation
{
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_a_message_is_sent : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_flow_causation_headers()
        {
            var context = await Scenario.Define<Context>()
                    .WithEndpoint<CausationEndpoint>(b => b.Given(bus => bus.SendLocalAsync(new MessageSentOutsideOfHandler())))
                    .Done(c => c.Done)
                    .Run();

            Assert.AreEqual(context.FirstConversationId, context.ConversationIdReceived, "Conversation id should flow to outgoing messages");
            Assert.AreEqual(context.MessageIdOfFirstMessage, context.RelatedToReceived, "RelatedToId on outgoing messages should be set to the message id of the message causing it to be sent");
        }


        public class Context : ScenarioContext
        {
            public bool Done { get; set; }
            public string FirstConversationId { get; set; }
            public string ConversationIdReceived { get; set; }
            public string MessageIdOfFirstMessage { get; set; }
            public string RelatedToReceived { get; set; }
        }

        public class CausationEndpoint : EndpointConfigurationBuilder
        {
            public CausationEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public Context Context { get; set; }


            public class MessageSentOutsideHandlersHandler : IHandleMessages<MessageSentOutsideOfHandler>
            {
                public IBus Bus { get; set; }

                public Context Context { get; set; }

                public Task Handle(MessageSentOutsideOfHandler message)
                {
                    Context.FirstConversationId = Bus.CurrentMessageContext.Headers[Headers.ConversationId];
                    Context.MessageIdOfFirstMessage = Bus.CurrentMessageContext.Id;

                    return Bus.SendLocalAsync(new MessageSentInsideHandler());
                }
            }

            public class MessageSentInsideHandlersHandler : IHandleMessages<MessageSentInsideHandler>
            {
                public IBus Bus { get; set; }

                public Context Context { get; set; }



                public Task Handle(MessageSentInsideHandler message)
                {
                    Context.ConversationIdReceived = Bus.CurrentMessageContext.Headers[Headers.ConversationId];

                    Context.RelatedToReceived = Bus.CurrentMessageContext.Headers[Headers.RelatedTo];

                    Context.Done = true;

                    return Task.FromResult(0);
                }
            }
        }

        public class MessageSentOutsideOfHandler : IMessage
        {
        }

        public class MessageSentInsideHandler : IMessage
        {
        }
    }
}
