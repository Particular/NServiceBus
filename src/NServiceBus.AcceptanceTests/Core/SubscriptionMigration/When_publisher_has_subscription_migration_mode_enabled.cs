namespace NServiceBus.AcceptanceTests.Core.SubscriptionMigration
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using Configuration.AdvancedExtensibility;
    using NUnit.Framework;

    public class When_publisher_has_subscription_migration_mode_enabled : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task CanHandleMessageDrivenSubscribers()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<MessageDrivenSubscriber>()
                .WithEndpoint<MigratedPublisher>(publisher => publisher
                    .When(ctx => Task.FromResult(ctx.Subscriber != null), (session, ctx) => session.Publish(new SomeEvent())))
                .Done(c => c.EventReceived)
                .Run();

            Assert.IsTrue(context.EventReceived);
            Assert.AreEqual(Conventions.EndpointNamingConvention(typeof(MessageDrivenSubscriber)), context.Subscriber);
        }

        [Test]
        public async Task CanHandleNativeSubscribers()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<NativeSubscriber>()
                .WithEndpoint<MigratedPublisher>(publisher => publisher
                    .When((session, ctx) => session.Publish(new SomeEvent())))
                .Done(c => c.EventReceived)
                .Run();

            Assert.IsTrue(context.EventReceived);
        }

        class Context : ScenarioContext
        {
            public string Subscriber { get; set; }
            public bool EventReceived { get; set; }
        }

        class MessageDrivenSubscriber : EndpointConfigurationBuilder
        {
            public MessageDrivenSubscriber()
            {
                EndpointSetup<EndpointWithMessageDrivenPubSub>(_ => { }, p => p.RegisterPublisherFor<SomeEvent>(typeof(MigratedPublisher)));
            }

            class SomeEventHandler : IHandleMessages<SomeEvent>
            {
                Context testContext;

                public SomeEventHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(SomeEvent message, IMessageHandlerContext context)
                {
                    testContext.EventReceived = true;
                    return Task.FromResult(0);
                }
            }
        }

        class NativeSubscriber : EndpointConfigurationBuilder
        {
            public NativeSubscriber()
            {
                EndpointSetup<EndpointWithNativePubSub>();
            }

            class SomeEventHandler : IHandleMessages<SomeEvent>
            {
                Context testContext;

                public SomeEventHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(SomeEvent message, IMessageHandlerContext context)
                {
                    testContext.EventReceived = true;
                    return Task.FromResult(0);
                }
            }
        }

        class MigratedPublisher : EndpointConfigurationBuilder
        {
            public MigratedPublisher()
            {
                EndpointSetup<EndpointWithNativePubSub>(c =>
                {
                    // Enable Migration mode
                    c.GetSettings().Set("NServiceBus.Subscriptions.EnableMigrationMode", true);
                    c.OnEndpointSubscribed<Context>((subscription, ctx) => { ctx.Subscriber = subscription.SubscriberEndpoint; });
                });
            }
        }

        public class SomeEvent : IEvent
        {
        }
    }
}