namespace NServiceBus.AcceptanceTests.Core.SubscriptionMigration;

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
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.EventReceived, Is.True);
            Assert.That(context.Subscriber, Is.EqualTo(Conventions.EndpointNamingConvention(typeof(MessageDrivenSubscriber))));
        }
    }

    [Test]
    public async Task CanHandleNativeSubscribers()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<NativeSubscriber>()
            .WithEndpoint<MigratedPublisher>(publisher => publisher
                .When((session, ctx) => session.Publish(new SomeEvent())))
            .Run();

        Assert.That(context.EventReceived, Is.True);
    }

    class Context : ScenarioContext
    {
        public string Subscriber { get; set; }
        public bool EventReceived { get; set; }
    }

    class MessageDrivenSubscriber : EndpointConfigurationBuilder
    {
        public MessageDrivenSubscriber() => EndpointSetup<EndpointWithMessageDrivenPubSub>(_ => { }, p => p.RegisterPublisherFor<SomeEvent, MigratedPublisher>());

        class SomeEventHandler(Context testContext) : IHandleMessages<SomeEvent>
        {
            public Task Handle(SomeEvent message, IMessageHandlerContext context)
            {
                testContext.EventReceived = true;
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }
    }

    class NativeSubscriber : EndpointConfigurationBuilder
    {
        public NativeSubscriber() => EndpointSetup<EndpointWithNativePubSub>();

        class SomeEventHandler(Context testContext) : IHandleMessages<SomeEvent>
        {
            public Task Handle(SomeEvent message, IMessageHandlerContext context)
            {
                testContext.EventReceived = true;
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }
    }

    class MigratedPublisher : EndpointConfigurationBuilder
    {
        public MigratedPublisher() =>
            EndpointSetup<EndpointWithNativePubSub>(c =>
            {
                // Enable Migration mode
                c.GetSettings().Set("NServiceBus.Subscriptions.EnableMigrationMode", true);
                c.OnEndpointSubscribed<Context>((subscription, ctx) => { ctx.Subscriber = subscription.SubscriberEndpoint; });
            });
    }

    public class SomeEvent : IEvent;
}