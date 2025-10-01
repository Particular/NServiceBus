namespace NServiceBus.AcceptanceTests.Core.Pipeline;

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

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.NotificationEventFired, Is.True, "ReceivePipelineCompleted was not raised");
            Assert.That(context.ReceivePipelineCompletedMessage.ProcessedMessage.MessageId, Is.EqualTo(context.MessageId), "MessageId mismatch");
            Assert.That(context.ReceivePipelineCompletedMessage.StartedAt, Is.Not.EqualTo(DateTimeOffset.MinValue), "StartedAt was not set");
            Assert.That(context.ReceivePipelineCompletedMessage.CompletedAt, Is.Not.EqualTo(DateTimeOffset.MinValue), "CompletedAt was not set");
        }
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
                c.Pipeline.OnReceivePipelineCompleted((e, _) =>
                {
                    var testContext = (Context)c.GetSettings().Get<ScenarioContext>();
                    testContext.ReceivePipelineCompletedMessage = e;
                    testContext.NotificationEventFired = true;
                    return Task.CompletedTask;
                });
            });
        }

        class SomeMessageHandler : IHandleMessages<SomeMessage>
        {
            public SomeMessageHandler(Context testContext)
            {
                this.testContext = testContext;
            }

            public Task Handle(SomeMessage message, IMessageHandlerContext context)
            {
                testContext.MessageId = context.MessageId;
                return Task.CompletedTask;
            }

            Context testContext;
        }
    }

    public class SomeMessage : IMessage
    {
    }
}
