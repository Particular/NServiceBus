using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTests.EndpointTemplates;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace NServiceBus.AcceptanceTests.Core.Causation
{
    public class When_starting_new_conversation_outside_message_handler : NServiceBusAcceptanceTest
    {
        const string NewConversionId = "User Defined Conversation Id";
        const string GeneratedConversationId = "Generated Conversation Id";

        [Test]
        public async Task With_specified_conversation_id()
        {
            var context = await Scenario.Define<NewConversationScenario>()
                .WithEndpoint<NewConversationEndpoint>(b => b.When(async session =>
                {
                    var sendOptions = new SendOptions();
                    sendOptions.RouteToThisEndpoint();
                    sendOptions.StartNewConversation(NewConversionId);

                    await session.Send(new AnyMessage(), sendOptions);
                }))
                .Done(ctx => ctx.MessageHandled)
                .Run();

            Assert.That(context.ConversationId, Is.EqualTo(NewConversionId), "ConversationId should be set to configured user defined value.");
            Assert.That(context.PreviousConversationId, Is.Null, "Previous ConversationId should not be set when handling a message outside of a message handler.");
        }

        [Test]
        public async Task Without_specified_conversation_id()
        {
            var context = await Scenario.Define<NewConversationScenario>()
                .WithEndpoint<NewConversationEndpoint>(b => b.When(async session =>
                {
                    var sendOptions = new SendOptions();
                    sendOptions.RouteToThisEndpoint();
                    sendOptions.StartNewConversation();

                    await session.Send(new AnyMessage(), sendOptions);
                }))
                .Done(ctx => ctx.MessageHandled)
                .Run();

            Assert.That(context.ConversationId, Is.EqualTo(GeneratedConversationId), "ConversationId should be generated.");
            Assert.That(context.ConversationId, Is.Not.EqualTo(context.PreviousConversationId), "ConversationId should not match the previous conversationId.");
            Assert.That(context.PreviousConversationId, Is.Null, "Previous ConversationId should not be set when handling a message outside of a message handler.");
        }

        [Test]
        public async Task Cannot_set_value_for_header_directly()
        {
            var overrideConversationId = "Some conversationid";

            var context = await Scenario.Define<NewConversationScenario>()
                .WithEndpoint<NewConversationEndpoint>(b => b.When(async (session, ctx) =>
                {
                    var sendOptions = new SendOptions();
                    sendOptions.RouteToThisEndpoint();
                    sendOptions.StartNewConversation();
                    sendOptions.SetHeader(Headers.ConversationId, overrideConversationId);

                    try
                    {
                        await session.Send(new AnyMessage(), sendOptions);
                    }
                    catch (Exception ex)
                    {
                        ctx.ExceptionMessage = ex.Message;
                        ctx.ExceptionThrown = true;
                    }
                }))
                .Done(ctx => ctx.ExceptionThrown)
                .Run();

            var expectedExceptionMessage = $"Cannot set the NServiceBus.ConversationId header to '{overrideConversationId}' as StartNewConversation() was called.";
            Assert.That(context.ExceptionThrown, Is.True, "Exception should be thrown when trying to directly set conversationId");
            Assert.That(context.ExceptionMessage, Is.EqualTo(expectedExceptionMessage));
        }

        public class AnyMessage : IMessage
        {

        }

        class NewConversationEndpoint : EndpointConfigurationBuilder
        {
            public NewConversationEndpoint()
            {
                EndpointSetup<DefaultServer>(c => c.CustomConversationIdStrategy(ctx => ConversationId.Custom(GeneratedConversationId)));
            }

            class AnyMessageHandler : IHandleMessages<AnyMessage>
            {
                private NewConversationScenario scenario;

                public AnyMessageHandler(NewConversationScenario scenario)
                {
                    this.scenario = scenario;
                }

                public Task Handle(AnyMessage message, IMessageHandlerContext context)
                {
                    if (context.MessageHeaders.TryGetValue(Headers.ConversationId, out var conversationId))
                    {
                        scenario.ConversationId = conversationId;
                    }

                    if (context.MessageHeaders.TryGetValue(Headers.PreviousConversationId, out var previousConversationId))
                    {
                        scenario.PreviousConversationId = previousConversationId;
                    }

                    scenario.MessageHandled = true;

                    return Task.FromResult(0);
                }
            }
        }

        class NewConversationScenario : ScenarioContext
        {
            public bool ExceptionThrown { get; set; }
            public string ExceptionMessage { get; set; }
            public bool MessageHandled { get; set; }
            public string ConversationId { get; set; }
            public string PreviousConversationId { get; set; }
        }
    }
}
