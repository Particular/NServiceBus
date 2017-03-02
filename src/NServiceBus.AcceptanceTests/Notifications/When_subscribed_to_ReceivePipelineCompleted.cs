namespace NServiceBus.AcceptanceTests.Notifications
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Configuration.AdvanceExtensibility;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;

    class When_subscribed_to_ReceivePipelineCompleted
    {
        [Test]
        public async Task Should_receive_then_notification()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<TestEndpoint>(b => b.When(session => session.SendLocal(new MessageToSend())))
                .Done(c => c.ReceivePipelineCompletedFired)
                .Run();

            Assert.True(context.ReceivePipelineCompletedFired, "ReceivePipelineCompleted was not received");
            Assert.AreEqual(context.MessageId, context.ReceivePipelineCompletedMessage.ProcessedMessage.MessageId, "MessageId mismatch");
            Assert.AreNotEqual(DateTime.MinValue, context.ReceivePipelineCompletedMessage.StartedAt, "StartedAt was not set");
            Assert.AreNotEqual(DateTime.MinValue, context.ReceivePipelineCompletedMessage.CompletedAt, "CompletedAt was not set");
        }

        class Context : ScenarioContext
        {
            public bool ReceivePipelineCompletedFired { get; set; }
            public ReceivePipelineCompleted ReceivePipelineCompletedMessage { get; set; }
            public string MessageId { get; set; }
        }

        class TestEndpoint : EndpointConfigurationBuilder
        {
            public TestEndpoint()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.Pipeline.OnReceivePipelineCompleted(e =>
                    {
                        var testContext = (Context) c.GetSettings().Get<ScenarioContext>();
                        testContext.ReceivePipelineCompletedMessage = e;
                        testContext.ReceivePipelineCompletedFired = true;
                        return Task.FromResult(0);
                    });
                });
            }

            class MessageToSendHandler : IHandleMessages<MessageToSend>
            {
                public Context TestContext { get; set; }

                public Task Handle(MessageToSend message, IMessageHandlerContext context)
                {
                    TestContext.MessageId = context.MessageId;
                    return Task.FromResult(0);
                }
            }
        }

        public class MessageToSend : IMessage
        {
        }
    }
}
