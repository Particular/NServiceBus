namespace NServiceBus.AcceptanceTests.Core.SubscriptionMigration
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using AcceptanceTests;
    using Configuration.AdvancedExtensibility;
    using NUnit.Framework;

    public class When_subscriber_has_subscription_migration_mode_enabled : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task CanSubscribeToMessageDrivenPublishers()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<MigratedSubscriber>()
                .WithEndpoint<MessageDrivenPublisher>(publisher => publisher
                    .When(ctx => Task.FromResult(ctx.Subscriber != null), (session, ctx) => session.Publish(new SomeEvent())))
                .Done(c => c.EventReceived)
                .Run();

            Assert.IsTrue(context.EventReceived);
            Assert.AreEqual(Conventions.EndpointNamingConvention(typeof(MigratedSubscriber)), context.Subscriber);
        }

        [Test]
        public async Task CanSubscribeToNativePublishers()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<MigratedSubscriber>()
                .WithEndpoint<NativePublisher>(publisher => publisher
                    .When((session, ctx) => session.Publish(new SomeEvent())))
                .Done(c => c.EventReceived)
                .Run();

            Assert.IsTrue(context.EventReceived);
        }

        class Context : ScenarioContext
        {
            public bool EventReceived { get; set; }
            public string Subscriber { get; set; }
        }

        class MigratedSubscriber : EndpointConfigurationBuilder
        {
            public MigratedSubscriber()
            {
                EndpointSetup<EndpointWithNativePubSub>(c =>
                {
                    // Enable Migration mode
                    c.GetSettings().Set("NServiceBus.Subscriptions.EnableMigrationMode", true);

                    var settings = new SubscriptionMigrationModeSettings(c.GetSettings());
                    settings.RegisterPublisher(typeof(SomeEvent), Conventions.EndpointNamingConvention(typeof(MessageDrivenPublisher)));
                });
            }

            class SomeEventHandler : IHandleMessages<SomeEvent>
            {
                Context testContext;

                public SomeEventHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(SomeEvent message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    testContext.EventReceived = true;
                    return Task.FromResult(0);
                }
            }
        }

        class MessageDrivenPublisher : EndpointConfigurationBuilder
        {
            public MessageDrivenPublisher()
            {
                EndpointSetup<EndpointWithMessageDrivenPubSub>(c => c.OnEndpointSubscribed<Context>((s, ctx) => ctx.Subscriber = s.SubscriberEndpoint));
            }
        }

        class NativePublisher : EndpointConfigurationBuilder
        {
            public NativePublisher()
            {
                EndpointSetup<EndpointWithNativePubSub>();
            }
        }

        public class SomeEvent : IEvent
        {
        }
    }
}