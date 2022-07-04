namespace NServiceBus.AcceptanceTests.Diagnostics;

using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Features;
using NUnit.Framework;

[NonParallelizable] // Ensure only activities for the current test are captured
public class When_subscribing : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_create_subscription_span_when_mdps()
    {
        Requires.MessageDrivenPubSub();

        using var activityListener = TestingActivityListener.SetupNServiceBusDiagnosticListener();

        var context = await Scenario.Define<Context>()
            .WithEndpoint<SubscribingEndpoint>(e => e
                .When(s => s.Subscribe<DemoEvent>()))
            .WithEndpoint<PublishingEndpoint>()
            .Done(c => c.Subscribed)
            .Run();

        var subscribeActivities = activityListener.CompletedActivities.Where(a => a.OperationName == "NServiceBus.Diagnostics.Subscribe")
            .ToArray();

        Assert.AreEqual(1, subscribeActivities.Length, "the subscriber should subscribe to the event");

        var subscribeActivity = subscribeActivities.Single();
        subscribeActivity.VerifyUniqueTags();
        Assert.AreEqual("subscribe event", subscribeActivity.DisplayName);
        var subscribeActivityTags = subscribeActivity.Tags.ToImmutableDictionary();
        subscribeActivityTags.VerifyTag("nservicebus.event_types", typeof(DemoEvent).FullName);

        var receiveActivities = activityListener.CompletedActivities.Where(a => a.OperationName == "NServiceBus.Diagnostics.IncomingMessage").ToArray();
        Assert.AreEqual(1, receiveActivities.Length, "the subscription message should be received by the publisher");
        Assert.AreEqual(subscribeActivities[0].Id, receiveActivities[0].ParentId, "the received subscription message should connect to the subscribe operation");
    }

    [Test]
    public async Task Should_create_subscription_span_when_native_pubsub()
    {
        Requires.NativePubSubSupport();

        using var activityListener = TestingActivityListener.SetupNServiceBusDiagnosticListener();

        var context = await Scenario.Define<Context>()
            .WithEndpoint<SubscribingEndpoint>(e => e
                .When(s => s.Subscribe<DemoEvent>()))
            .WithEndpoint<PublishingEndpoint>()
            .Done(c => c.EndpointsStarted)
            .Run();

        var subscribeActivities = activityListener.CompletedActivities.Where(a => a.OperationName == "NServiceBus.Diagnostics.Subscribe")
            .ToArray();

        Assert.AreEqual(1, subscribeActivities.Length, "the subscriber should subscribe to the event");

        var subscribeActivity = subscribeActivities.Single();
        subscribeActivity.VerifyUniqueTags();
        Assert.AreEqual("subscribe event", subscribeActivity.DisplayName);
        var subscribeActivityTags = subscribeActivity.Tags.ToImmutableDictionary();
        subscribeActivityTags.VerifyTag("nservicebus.event_types", typeof(DemoEvent).FullName);

        var subscriptionReceiveActivity = activityListener.CompletedActivities.GetIncomingActivities();
        Assert.IsEmpty(subscriptionReceiveActivity, "native pubsub should not produce a message");
    }

    class Context : ScenarioContext
    {
        public bool Subscribed { get; set; }
    }

    class SubscribingEndpoint : EndpointConfigurationBuilder
    {
        public SubscribingEndpoint() => EndpointSetup<DefaultServer>(
            c => c.DisableFeature<AutoSubscribe>(),
            p => p.RegisterPublisherFor<DemoEvent>(typeof(PublishingEndpoint)));
    }

    class PublishingEndpoint : EndpointConfigurationBuilder
    {
        public PublishingEndpoint() => EndpointSetup<DefaultServer>(c =>
        {
            c.DisableFeature<AutoSubscribe>();
            c.OnEndpointSubscribed<Context>((e, ctx) =>
            {
                if (e.MessageType == typeof(DemoEvent).AssemblyQualifiedName)
                {
                    ctx.Subscribed = true;
                }
            });
        });
    }

    public class DemoEvent : IEvent
    {
    }
}