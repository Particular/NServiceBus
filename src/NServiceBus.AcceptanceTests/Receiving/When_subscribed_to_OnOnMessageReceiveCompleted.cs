namespace NServiceBus.AcceptanceTests.Core.Pipeline
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NServiceBus.Transport;
    using NUnit.Framework;

    class When_subscribed_to_OnReceiveCompleted : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_receive_when_OnMessage_succeeds()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<SubscribingEndpoint>(b =>
                    b.When(session =>
                    {
                        var options = new SendOptions();

                        options.RouteToThisEndpoint();
                        options.SetHeader("SomeKey", "SomeValue");

                        return session.Send(new SomeMessage(), options);
                    }))
                .Done(c => c.ReceiveCompleted != null)
                .Run();

            Assert.NotNull(context.ReceiveCompleted, "Event was not received");
            Assert.AreEqual(context.NativeMessageId, context.ReceiveCompleted.NativeMessageId, "Event native message ID does not match message native message ID");
            Assert.AreEqual(ReceiveResult.Processed, context.ReceiveCompleted.Result, "Event indicates that message was not processed");
            Assert.AreNotEqual(DateTime.MinValue, context.ReceiveCompleted.StartedAt, "StartedAt is not set");
            Assert.AreNotEqual(DateTime.MinValue, context.ReceiveCompleted.CompletedAt, "CompletedAt is not set");
            Assert.True(context.ReceiveCompleted.Headers.ContainsKey("SomeKey"));
            Assert.AreEqual(context.ReceiveCompleted.Headers["SomeKey"], "SomeValue");
        }

        [Test]
        public async Task Should_receive_when_OnMessage_fails()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<SubscribingEndpoint>(b =>
                {
                    // do one immediate retry to check that message is rolled back
                    b.CustomConfig(c => c.Recoverability().Immediate(i => i.NumberOfRetries(1)));

                    // message will then go to error so we need to allow that
                    b.DoNotFailOnErrorMessages();

                    b.When(session =>
                    {
                        var options = new SendOptions();

                        options.RouteToThisEndpoint();
                        options.SetHeader("SomeKey", "SomeValue");

                        return session.Send(new SomeMessage { Throw = true }, options);
                    });
                })
                .Done(c => c.ReceiveCompleted != null)
                .Run();

            Assert.NotNull(context.ReceiveCompleted, "Event was not received");
            Assert.AreEqual(context.NativeMessageId, context.ReceiveCompleted.NativeMessageId, "Event native message ID does not match message native message ID");
            Assert.AreEqual(ReceiveResult.MovedToErrorQueue, context.ReceiveCompleted.Result, "Event indicates that message was not moved to error queue");
            Assert.AreNotEqual(DateTime.MinValue, context.ReceiveCompleted.StartedAt, "StartedAt is not set");

            Assert.AreNotEqual(DateTime.MinValue, context.ReceiveCompleted.CompletedAt, "CompletedAt is not set");
            Assert.True(context.ReceiveCompleted.Headers.ContainsKey("SomeKey"));
            Assert.AreEqual(context.ReceiveCompleted.Headers["SomeKey"], "SomeValue");
        }

        class Context : ScenarioContext
        {
            public ReceiveCompleted ReceiveCompleted { get; set; }

            public string NativeMessageId { get; set; }
        }

        class SubscribingEndpoint : EndpointConfigurationBuilder
        {
            public SubscribingEndpoint() => EndpointSetup<DefaultServer>();

            class EventEnablingFeature : Feature
            {
                public EventEnablingFeature() => EnableByDefault();

                protected override void Setup(FeatureConfigurationContext context) =>
                    context.OnReceiveCompleted((receiveCompleted, _) =>
                        {
                            var testContext = (Context)context.Settings.Get<ScenarioContext>();

                            if (testContext.ReceiveCompleted == null)
                            {
                                testContext.ReceiveCompleted = receiveCompleted;
                            }

                            return Task.CompletedTask;
                        });
            }

            class SomeMessageHandler : IHandleMessages<SomeMessage>
            {
                public SomeMessageHandler(Context testContext) => this.testContext = testContext;

                public Task Handle(SomeMessage message, IMessageHandlerContext context)
                {
                    if (testContext.ReceiveCompleted != null)
                    {
                        return Task.CompletedTask;
                    }

                    testContext.NativeMessageId = context.Extensions.Get<IncomingMessage>().NativeMessageId;

                    if (message.Throw)
                    {
                        throw new Exception("Simulated failure");
                    }

                    return Task.CompletedTask;
                }

                readonly Context testContext;
            }
        }

        public class SomeMessage : IMessage
        {
            public bool Throw { get; set; }
        }
    }
}
