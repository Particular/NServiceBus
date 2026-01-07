namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry.Traces;

using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using EndpointTemplates;
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
            .Run();

        var unsubscribeActivities = NServiceBusActivityListener.CompletedActivities.Where(a => a.OperationName == "NServiceBus.Diagnostics.Unsubscribe")
            .ToArray();
        Assert.That(unsubscribeActivities.Length, Is.EqualTo(1), "the subscriber should unsubscribe to the event");

        var unsubscribeActivity = unsubscribeActivities.Single();
        unsubscribeActivity.VerifyUniqueTags();
        Assert.That(unsubscribeActivity.DisplayName, Is.EqualTo("unsubscribe event"));

        var unsubscribeActivityTags = unsubscribeActivity.Tags.ToImmutableDictionary();
        unsubscribeActivityTags.VerifyTag("nservicebus.event_types", typeof(DemoEvent).FullName);

        var receiveActivities = NServiceBusActivityListener.CompletedActivities.GetReceiveMessageActivities(includeControlMessages: true).ToArray();
        Assert.That(receiveActivities.Length, Is.EqualTo(1), "the unsubscribe message should be received by the publisher");
        Assert.That(receiveActivities[0].ParentId, Is.EqualTo(unsubscribeActivities[0].Id), "the received unsubscribe message should connect to the subscribe operation");
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

        var unsubscribeActivities = NServiceBusActivityListener.CompletedActivities.Where(a => a.OperationName == "NServiceBus.Diagnostics.Unsubscribe").ToArray();
        Assert.That(unsubscribeActivities.Length, Is.EqualTo(1), "the subscriber should unsubscribe to the event");

        var unsubscribeActivity = unsubscribeActivities.Single();
        unsubscribeActivity.VerifyUniqueTags();
        Assert.That(unsubscribeActivity.DisplayName, Is.EqualTo("unsubscribe event"));

        var unsubscribeActivityTags = unsubscribeActivity.Tags.ToImmutableDictionary();
        unsubscribeActivityTags.VerifyTag("nservicebus.event_types", typeof(DemoEvent).FullName);

        var subscriptionReceiveActivity = NServiceBusActivityListener.CompletedActivities.GetReceiveMessageActivities(includeControlMessages: true);
        Assert.That(subscriptionReceiveActivity, Is.Empty, "native pubsub should not produce a message");
    }

    class Context : ScenarioContext;

    class SubscriberEndpoint : EndpointConfigurationBuilder
    {
        public SubscriberEndpoint() => EndpointSetup<DefaultServer>(
            c => c.DisableFeature<AutoSubscribe>(),
            p => p.RegisterPublisherFor<DemoEvent, PublishingEndpoint>());
    }

    class PublishingEndpoint : EndpointConfigurationBuilder
    {
        public PublishingEndpoint() => EndpointSetup<DefaultServer>(c =>
        {
            c.DisableFeature<AutoSubscribe>();
            c.OnEndpointUnsubscribed<Context>((e, ctx) =>
            {
                if (e.MessageType == typeof(DemoEvent).AssemblyQualifiedName)
                {
                    ctx.MarkAsCompleted();
                }
            });
        });
    }

    public class DemoEvent : IEvent;
}