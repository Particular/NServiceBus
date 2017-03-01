namespace NServiceBus.AcceptanceTests.Notifications
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;
    
    class When_Subscribing_To_Notifications
    {
        [Test]
        public async Task Can_Subscribe_To_ReceivePipelineCompleted()
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

        class MessageToSend : ICommand
        {
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
                EndpointSetup<DefaultServer>();
            }

            class NotificationsSubscriptionFeature : Feature
            {
                public NotificationsSubscriptionFeature()
                {
                    EnableByDefault();
                }

                protected override void Setup(FeatureConfigurationContext context)
                {
                    var notifications = context.Settings.Get<NotificationSubscriptions>();

                    notifications.Subscribe<ReceivePipelineCompleted>(e =>
                    {
                        var testContext = (Context)context.Settings.Get<ScenarioContext>();
                        testContext.ReceivePipelineCompletedFired = true;
                        testContext.ReceivePipelineCompletedMessage = e;
                        return Task.FromResult(0);
                    });
                }
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
    }
}
