namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry;

using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus.AcceptanceTesting;
using NServiceBus.Features;
using NUnit.Framework;

public class When_unsubscribing : OpenTelemetryAcceptanceTest
{
    [Test]
    public async Task Should_create_unsubscribe_span_when_mdps()
    {
        Requires.MessageDrivenPubSub();

        var context = await Scenario.Define<Context>()
            .WithEndpoint<SubscriberEndpoint>(e => e
                .When(s => s.Unsubscribe<DemoEvent>()))
            .WithEndpoint<PublishingEndpoint>()
            .Done(c => c.Unsubscribed)
            .Run();

        var unsubscribeActivities = NServicebusActivityListener.CompletedActivities.Where(a => a.OperationName == "NServiceBus.Diagnostics.Unsubscribe")
            .ToArray();
        Assert.AreEqual(1, unsubscribeActivities.Length, "the subscriber should unsubscribe to the event");

        var unsubscribeActivity = unsubscribeActivities.Single();
        unsubscribeActivity.VerifyUniqueTags();
        Assert.AreEqual("unsubscribe event", unsubscribeActivity.DisplayName);

        var unsubscribeActivityTags = unsubscribeActivity.Tags.ToImmutableDictionary();
        unsubscribeActivityTags.VerifyTag("nservicebus.event_types", typeof(DemoEvent).FullName);

        var receiveActivities = NServicebusActivityListener.CompletedActivities.GetReceiveMessageActivities(includeControlMessages: true).ToArray();
        Assert.AreEqual(1, receiveActivities.Length, "the unsubscribe message should be received by the publisher");
        Assert.AreEqual(unsubscribeActivities[0].Id, receiveActivities[0].ParentId, "the received unsubscribe message should connect to the subscribe operation");
    }

    [Test]
    public async Task Should_create_unsubscribe_span_when_native_pubsub()
    {
        Requires.NativePubSubSupport();

        var context = await Scenario.Define<Context>()
            .WithEndpoint<SubscriberEndpoint>(e => e
                .When(s => s.Unsubscribe<DemoEvent>()))
            .WithEndpoint<PublishingEndpoint>()
            .Done(c => c.EndpointsStarted)
            .Run();

        var unsubscribeActivities = NServicebusActivityListener.CompletedActivities.Where(a => a.OperationName == "NServiceBus.Diagnostics.Unsubscribe").ToArray();
        Assert.AreEqual(1, unsubscribeActivities.Length, "the subscriber should unsubscribe to the event");

        var unsubscribeActivity = unsubscribeActivities.Single();
        unsubscribeActivity.VerifyUniqueTags();
        Assert.AreEqual("unsubscribe event", unsubscribeActivity.DisplayName);

        var unsubscribeActivityTags = unsubscribeActivity.Tags.ToImmutableDictionary();
        unsubscribeActivityTags.VerifyTag("nservicebus.event_types", typeof(DemoEvent).FullName);

        var subscriptionReceiveActivity = NServicebusActivityListener.CompletedActivities.GetReceiveMessageActivities(includeControlMessages: true);
        Assert.IsEmpty(subscriptionReceiveActivity, "native pubsub should not produce a message");
    }

    class Context : ScenarioContext
    {
        public bool Unsubscribed { get; set; }
    }

    class SubscriberEndpoint : EndpointConfigurationBuilder
    {
        public SubscriberEndpoint() => EndpointSetup<OpenTelemetryEnabledEndpoint>(
            c => c.DisableFeature<AutoSubscribe>(),
            p => p.RegisterPublisherFor<DemoEvent>(typeof(PublishingEndpoint)));
    }

    class PublishingEndpoint : EndpointConfigurationBuilder
    {
        public PublishingEndpoint() => EndpointSetup<OpenTelemetryEnabledEndpoint>(c =>
        {
            c.DisableFeature<AutoSubscribe>();
            c.OnEndpointUnsubscribed<Context>((e, ctx) =>
            {
                if (e.MessageType == typeof(DemoEvent).AssemblyQualifiedName)
                {
                    ctx.Unsubscribed = true;
                }
            });
        });
    }

    public class DemoEvent : IEvent
    {
    }
}