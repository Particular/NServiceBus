namespace NServiceBus.AcceptanceTests.Core.Pipeline
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Configuration.AdvanceExtensibility;
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

        [Test]
        public async Task Should_receive_notification_when_handler_fails()
        {
            await Scenario.Define<Context>()
                .WithEndpoint<SubscribingEndpoint>(b =>
                {
                    b.When(session => session.SendLocal(new FailingMessage()));
                    b.DoNotFailOnErrorMessages();
                })
                .Done(c => c.NotificationEventFired)
                .Run();
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
                public Context TestContext { get; set; }

                public Task Handle(SomeMessage message, IMessageHandlerContext context)
                {
                    TestContext.MessageId = context.MessageId;
                    return Task.FromResult(0);
                }
            }

            class FalingMessageHandler : IHandleMessages<FailingMessage>
            {
                public Task Handle(FailingMessage message, IMessageHandlerContext context)
                {
                    throw new NotImplementedException();
                }
            }
        }

        public class SomeMessage : IMessage
        {
        }

        public class FailingMessage : IMessage
        {

        }
    }
}
