using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTesting.Customization;
using NServiceBus.AcceptanceTests.EndpointTemplates;
using NUnit.Framework;
using System.Threading.Tasks;

namespace NServiceBus.AcceptanceTests.Core.Causation
{
    public class When_starting_new_conversation_inside_message_handler : NServiceBusAcceptanceTest
    {
        const string GeneratedConversationId = "Generated Conversation Id";
        const string UserDefinedConverstionId = "User Definied Conversation Id";

        [Test]
        public async Task With_specified_conversation_id()
        {
            var context = await Scenario.Define<NewConversationScenario>(ctx => ctx.PropsedConversationId = UserDefinedConverstionId)
                .WithEndpoint<Sender>(b => b.When(session =>
                {
                    return session.Send(new AnyMessage());
                }))
                .WithEndpoint<Receiver>()
                .Done(ctx => ctx.MessageHandled)
                .Run();

            Assert.That(context.NewConversationId, Is.EqualTo(UserDefinedConverstionId), "ConversationId should be set to the user defined value.");
            Assert.That(context.PreviousConversationId, Is.EqualTo(context.OriginalConversationId), "PreviousConversationId header should be set to the original conversation id.");
        }

        [Test]
        public async Task Without_specified_conversation_id()
        {
            var context = await Scenario.Define<NewConversationScenario>()
                .WithEndpoint<Sender>(b => b.When(session =>
                {
                    return session.Send(new AnyMessage());
                }))
                .WithEndpoint<Receiver>()
                .Done(ctx => ctx.MessageHandled)
                .Run();

            Assert.That(context.NewConversationId, Is.EqualTo(GeneratedConversationId), "ConversationId should be generated.");
            Assert.That(context.NewConversationId, Is.Not.EqualTo(context.OriginalConversationId), "ConversationId should not be equal to the original conversation id.");
            Assert.That(context.PreviousConversationId, Is.EqualTo(context.OriginalConversationId), "PreviousConversationId header should be set to the original conversation id.");
        }

        class NewConversationScenario : ScenarioContext
        {
            public string PropsedConversationId { get; set; }
            public bool MessageHandled { get; set; }
            public string OriginalConversationId { get; set; }
            public string NewConversationId { get; set; }
            public string PreviousConversationId { get; set; }
        }

        class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>(
                    c => c.ConfigureTransport()
                          .Routing()
                          .RouteToEndpoint(typeof(AnyMessage), typeof(Receiver)));
            }

            class AnyResponseMessageHandler : IHandleMessages<AnyResponseMessage>
            {
                NewConversationScenario scenario;

                public AnyResponseMessageHandler(NewConversationScenario scenario)
                {
                    this.scenario = scenario;
                }

                public Task Handle(AnyResponseMessage message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    if(context.MessageHeaders.TryGetValue(Headers.ConversationId ,out var conversationId))
                    {
                        scenario.NewConversationId = conversationId;
                    }
                    if(context.MessageHeaders.TryGetValue(Headers.PreviousConversationId, out var previousConversationId))
                    {
                        scenario.PreviousConversationId = previousConversationId;
                    }
                    scenario.MessageHandled = true;
                    return Task.FromResult(0);
                }
            }
        }

        class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>(
                    c =>
                    {
                        c.ConfigureTransport()
                              .Routing()
                              .RouteToEndpoint(typeof(AnyResponseMessage), typeof(Sender));

                        c.CustomConversationIdStrategy(ctx => ConversationId.Custom(GeneratedConversationId));
                    });
            }

            class AnyMessageHandler : IHandleMessages<AnyMessage>
            {
                NewConversationScenario scenario;

                public AnyMessageHandler(NewConversationScenario scenario)
                {
                    this.scenario = scenario;
                }

                public Task Handle(AnyMessage message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    scenario.OriginalConversationId = context.MessageHeaders[Headers.ConversationId];

                    var sendOptions = new SendOptions();
                    sendOptions.StartNewConversation(scenario.PropsedConversationId);

                    return context.Send(new AnyResponseMessage(), sendOptions);
                }
            }
        }

        public class AnyMessage : IMessage { }
        public class AnyResponseMessage : IMessage { }
    }
}
