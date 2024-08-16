namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry.Traces;

using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus.AcceptanceTesting;
using NServiceBus.Features;
using NUnit.Framework;

public class When_subscribing : OpenTelemetryAcceptanceTest
{
    [Test]
    public async Task Should_create_subscription_span_when_mdps()
    {
        Requires.MessageDrivenPubSub();

        var context = await Scenario.Define<Context>()
            .WithEndpoint<SubscribingEndpoint>(e => e
                .When(s => s.Subscribe<DemoEvent>()))
            .WithEndpoint<PublishingEndpoint>()
            .Done(c => c.Subscribed)
            .Run();

        var subscribeActivities = NServicebusActivityListener.CompletedActivities.Where(a => a.OperationName == "NServiceBus.Diagnostics.Subscribe")
            .ToArray();

        Assert.That(subscribeActivities.Length, Is.EqualTo(1), "the subscriber should subscribe to the event");

        var subscribeActivity = subscribeActivities.Single();
        subscribeActivity.VerifyUniqueTags();
        Assert.That(subscribeActivity.DisplayName, Is.EqualTo("subscribe event"));
        var subscribeActivityTags = subscribeActivity.Tags.ToImmutableDictionary();
        subscribeActivityTags.VerifyTag("nservicebus.event_types", typeof(DemoEvent).FullName);

        var receiveActivities = NServicebusActivityListener.CompletedActivities.GetReceiveMessageActivities(includeControlMessages: true).ToArray();
        Assert.That(receiveActivities.Length, Is.EqualTo(1), "the subscription message should be received by the publisher");
        Assert.That(receiveActivities[0].ParentId, Is.EqualTo(subscribeActivities[0].Id), "the received subscription message should connect to the subscribe operation");
    }

    [Test]
    public async Task Should_create_subscription_span_when_native_pubsub()
    {
        Requires.NativePubSubSupport();

        var context = await Scenario.Define<Context>()
            .WithEndpoint<SubscribingEndpoint>(e => e
                .When(s => s.Subscribe<DemoEvent>()))
            .WithEndpoint<PublishingEndpoint>()
            .Done(c => c.EndpointsStarted)
            .Run();

        var subscribeActivities = NServicebusActivityListener.CompletedActivities.Where(a => a.OperationName == "NServiceBus.Diagnostics.Subscribe")
            .ToArray();

        Assert.That(subscribeActivities.Length, Is.EqualTo(1), "the subscriber should subscribe to the event");

        var subscribeActivity = subscribeActivities.Single();
        subscribeActivity.VerifyUniqueTags();
        Assert.That(subscribeActivity.DisplayName, Is.EqualTo("subscribe event"));
        var subscribeActivityTags = subscribeActivity.Tags.ToImmutableDictionary();
        subscribeActivityTags.VerifyTag("nservicebus.event_types", typeof(DemoEvent).FullName);

        var subscriptionReceiveActivity = NServicebusActivityListener.CompletedActivities.GetReceiveMessageActivities(includeControlMessages: true);
        Assert.IsEmpty(subscriptionReceiveActivity, "native pubsub should not produce a message");
    }

    class Context : ScenarioContext
    {
        public bool Subscribed { get; set; }
    }

    class SubscribingEndpoint : EndpointConfigurationBuilder
    {
        public SubscribingEndpoint() => EndpointSetup<OpenTelemetryEnabledEndpoint>(
            c => c.DisableFeature<AutoSubscribe>(),
            p => p.RegisterPublisherFor<DemoEvent>(typeof(PublishingEndpoint)));
    }

    class PublishingEndpoint : EndpointConfigurationBuilder
    {
        public PublishingEndpoint() => EndpointSetup<OpenTelemetryEnabledEndpoint>(c =>
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