namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry
{
    using System;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Pipeline;
    using NUnit.Framework;

    public class When_sending_messages_with_custom_activity : OpenTelemetryAcceptanceTest
    {
        static ActivitySource externalActivitySource = new(Guid.NewGuid().ToString());

        [Test]
        public async Task Should_add_tags_to_the_existing_activity()
        {
            using var activityListener = TestingActivityListener.SetupNServiceBusDiagnosticListener();
            using var customActivityListener = TestingActivityListener.SetupDiagnosticListener(externalActivitySource.Name); // need to have a registered listener for activities to be created

            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithActivity>(e => e
                    .When(s => s.SendLocal(new TriggerMessage())))
                .Done(c => c.MessageReceived)
                .Run();

            var sendActivity = activityListener.CompletedActivities.GetOutgoingActivities().First();
            var customActivity = customActivityListener.CompletedActivities.First();

            var sendActivityTags = sendActivity.Tags.ToImmutableDictionary();
            var customActivityTags = customActivity.Tags.ToImmutableDictionary();

            VerifyTagDoesNotExist("nservicebus.message_id");
            VerifyTagDoesNotExist("nservicebus.conversation_id");

            VerifyTag("nservicebus.message_id");
            VerifyTag("nservicebus.conversation_id");

            void VerifyTag(string tagKey)
            {
                Assert.IsTrue(sendActivityTags.TryGetKey(tagKey, out var tagValue), $"Tags should contain key {tagKey}");
            }

            void VerifyTagDoesNotExist(string tagKey)
            {
                Assert.IsFalse(customActivityTags.Keys.Contains(tagKey), $"custom activity contains unexpected tag: '{tagKey}'");
            }
        }

        public class Context : ScenarioContext
        {
            public bool MessageReceived { get; set; }
        }

        class EndpointWithActivity : EndpointConfigurationBuilder
        {
            public EndpointWithActivity()
            {
                EndpointSetup<DefaultServer, Context>((config, context) =>
                {
                    config.Pipeline.Register(typeof(CustomActivityBehavior), "Simulate a behavior with a custom activity");
                });
            }

            public class MessageHandler : IHandleMessages<TriggerMessage>
            {
                Context testContext;

                public MessageHandler(Context testContext) => this.testContext = testContext;

                public Task Handle(TriggerMessage message, IMessageHandlerContext context)
                {
                    testContext.MessageReceived = true;
                    return Task.CompletedTask;
                }
            }
        }

        class CustomActivityBehavior : Behavior<IOutgoingLogicalMessageContext>
        {
            public override async Task Invoke(IOutgoingLogicalMessageContext context, Func<Task> next)
            {
                using (var ambientActivity = externalActivitySource.StartActivity())
                {
                    await next();
                }
            }
        }

        public class TriggerMessage : IMessage
        {
        }
    }
}