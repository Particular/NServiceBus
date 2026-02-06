namespace NServiceBus.AcceptanceTests.Core.Causation;

using System.Threading.Tasks;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTesting.Customization;
using NServiceBus.AcceptanceTests.EndpointTemplates;
using NUnit.Framework;

public class When_starting_new_conversation_inside_message_handler : NServiceBusAcceptanceTest
{
    const string GeneratedConversationId = "Generated Conversation Id";
    const string UserDefinedConverstionId = "User Definied Conversation Id";

    [Test]
    public async Task With_specified_conversation_id()
    {
        var context = await Scenario.Define<NewConversationScenario>(ctx => ctx.PropsedConversationId = UserDefinedConverstionId)
            .WithEndpoint<Sender>(b => b.When(session => session.Send(new AnyMessage())))
            .WithEndpoint<Receiver>()
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.NewConversationId, Is.EqualTo(UserDefinedConverstionId), "ConversationId should be set to the user defined value.");
            Assert.That(context.PreviousConversationId, Is.EqualTo(context.OriginalConversationId), "PreviousConversationId header should be set to the original conversation id.");
        }
    }

    [Test]
    public async Task Without_specified_conversation_id()
    {
        var context = await Scenario.Define<NewConversationScenario>()
            .WithEndpoint<Sender>(b => b.When(session => session.Send(new AnyMessage())))
            .WithEndpoint<Receiver>()
            .Run();

        Assert.That(context.NewConversationId, Is.EqualTo(GeneratedConversationId), "ConversationId should be generated.");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.NewConversationId, Is.Not.EqualTo(context.OriginalConversationId), "ConversationId should not be equal to the original conversation id.");
            Assert.That(context.PreviousConversationId, Is.EqualTo(context.OriginalConversationId), "PreviousConversationId header should be set to the original conversation id.");
        }
    }

    public class NewConversationScenario : ScenarioContext
    {
        public string PropsedConversationId { get; set; }
        public string OriginalConversationId { get; set; }
        public string NewConversationId { get; set; }
        public string PreviousConversationId { get; set; }
    }

    public class Sender : EndpointConfigurationBuilder
    {
        public Sender() =>
            EndpointSetup<DefaultServer>(
                c => c.ConfigureRouting()
                    .RouteToEndpoint(typeof(AnyMessage), typeof(Receiver)));

        [Handler]
        public class AnyResponseMessageHandler(NewConversationScenario scenario) : IHandleMessages<AnyResponseMessage>
        {
            public Task Handle(AnyResponseMessage message, IMessageHandlerContext context)
            {
                if (context.MessageHeaders.TryGetValue(Headers.ConversationId, out var conversationId))
                {
                    scenario.NewConversationId = conversationId;
                }
                if (context.MessageHeaders.TryGetValue(Headers.PreviousConversationId, out var previousConversationId))
                {
                    scenario.PreviousConversationId = previousConversationId;
                }
                scenario.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class Receiver : EndpointConfigurationBuilder
    {
        public Receiver() =>
            EndpointSetup<DefaultServer>(
                c =>
                {
                    c.ConfigureRouting()
                        .RouteToEndpoint(typeof(AnyResponseMessage), typeof(Sender));

                    c.CustomConversationIdStrategy(ctx => ConversationId.Custom(GeneratedConversationId));
                });

        [Handler]
        public class AnyMessageHandler(NewConversationScenario scenario) : IHandleMessages<AnyMessage>
        {
            public Task Handle(AnyMessage message, IMessageHandlerContext context)
            {
                scenario.OriginalConversationId = context.MessageHeaders[Headers.ConversationId];

                var sendOptions = new SendOptions();
                sendOptions.StartNewConversation(scenario.PropsedConversationId);

                return context.Send(new AnyResponseMessage(), sendOptions);
            }
        }
    }

    public class AnyMessage : IMessage;
    public class AnyResponseMessage : IMessage;
}
