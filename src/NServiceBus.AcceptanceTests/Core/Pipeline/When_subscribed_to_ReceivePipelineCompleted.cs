namespace NServiceBus.AcceptanceTests.Core.Pipeline
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Configuration.AdvancedExtensibility;
    using EndpointTemplates;
    using NUnit.Framework;

    class When_subscribed_to_ReceivePipelineCompleted : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_receive_notifications()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<SubscribingEndpoint>(b => b.When(session => session.SendLocal(new SomeMessage())))
                .Done(c => c.NotificationEventFired)
                .Run();

            Assert.True(context.NotificationEventFired, "ReceivePipelineCompleted was not raised");
            Assert.AreEqual(context.MessageId, context.ReceivePipelineCompletedMessage.ProcessedMessage.MessageId, "MessageId mismatch");
            Assert.AreNotEqual(DateTime.MinValue, context.ReceivePipelineCompletedMessage.StartedAt, "StartedAt was not set");
            Assert.AreNotEqual(DateTime.MinValue, context.ReceivePipelineCompletedMessage.CompletedAt, "CompletedAt was not set");
        }

        class Context : ScenarioContext
        {
            public bool NotificationEventFired { get; set; }
            public ReceivePipelineCompleted ReceivePipelineCompletedMessage { get; set; }
            public string MessageId { get; set; }
        }

        class SubscribingEndpoint : EndpointConfigurationBuilder
        {
            public SubscribingEndpoint()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.Pipeline.OnReceivePipelineCompleted(e =>
                    {
                        var testContext = (Context)c.GetSettings().Get<ScenarioContext>();
                        testContext.ReceivePipelineCompletedMessage = e;
                        testContext.NotificationEventFired = true;
                        return Task.FromResult(0);
                    });
                });
            }

            class SomeMessageHandler : IHandleMessages<SomeMessage>
            {
                public SomeMessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(SomeMessage message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    testContext.MessageId = context.MessageId;
                    return Task.FromResult(0);
                }

                Context testContext;
            }
        }

        public class SomeMessage : IMessage
        {
        }
    }
}
