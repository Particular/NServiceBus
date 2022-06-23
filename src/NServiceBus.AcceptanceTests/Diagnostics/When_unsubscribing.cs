namespace NServiceBus.AcceptanceTests.Diagnostics;

using System.Linq;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Features;
using NUnit.Framework;

[NonParallelizable] // Ensure only activities for the current test are captured
public class When_unsubscribing : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_create_unsubscribe_span_when_mdps()
    {
        Requires.MessageDrivenPubSub();

        using var activityListener = TestingActivityListener.SetupNServiceBusDiagnosticListener();

        var context = await Scenario.Define<Context>()
            .WithEndpoint<SubscriberEndpoint>(e => e
                .When(s => s.Unsubscribe<DemoEvent>()))
            .WithEndpoint<PublishingEndpoint>()
            .Done(c => c.Unsubscribed)
            .Run();

        var unsubscribeActivities = activityListener.CompletedActivities.Where(a => a.OperationName == "NServiceBus.Diagnostics.Unsubscribe")
            .ToArray();
        Assert.AreEqual(1, unsubscribeActivities.Length, "the subscriber should unsubscribe to the event");

        var unsubscribeActivity = unsubscribeActivities.Single();
        unsubscribeActivity.VerifyUniqueTags();
        Assert.AreEqual("unsubscribe", unsubscribeActivity.DisplayName);

        //TODO assert tags etc.

        var receiveActivities = activityListener.CompletedActivities.Where(a => a.OperationName == "NServiceBus.Diagnostics.IncomingMessage").ToArray();
        Assert.AreEqual(1, receiveActivities.Length, "the unsubscribe message should be received by the publisher");
        Assert.AreEqual(unsubscribeActivities[0].Id, receiveActivities[0].ParentId, "the received unsubscribe message should connect to the subscribe operation");
    }

    [Test]
    public async Task Should_create_unsubscribe_span_when_native_pubsub()
    {
        Requires.NativePubSubSupport();

        using var activityListener = TestingActivityListener.SetupNServiceBusDiagnosticListener();

        var context = await Scenario.Define<Context>()
            .WithEndpoint<SubscriberEndpoint>(e => e
                .When(s => s.Unsubscribe<DemoEvent>()))
            .WithEndpoint<PublishingEndpoint>()
            .Done(c => c.EndpointsStarted)
            .Run();

        var unsubscribeActivities = activityListener.CompletedActivities.Where(a => a.OperationName == "NServiceBus.Diagnostics.Unsubscribe").ToArray();
        Assert.AreEqual(1, unsubscribeActivities.Length, "the subscriber should unsubscribe to the event");

        var unsubscribeActivity = unsubscribeActivities.Single();
        unsubscribeActivity.VerifyUniqueTags();
        Assert.AreEqual("unsubscribe", unsubscribeActivity.DisplayName);

        //TODO assert tags etc.

        var subscriptionReceiveActivity = activityListener.CompletedActivities.GetIncomingActivities();
        Assert.IsEmpty(subscriptionReceiveActivity, "native pubsub should not produce a message");
    }

    class Context : ScenarioContext
    {
        public bool Unsubscribed { get; set; }
    }

    class SubscriberEndpoint : EndpointConfigurationBuilder
    {
        public SubscriberEndpoint() => EndpointSetup<DefaultServer>(
            c => c.DisableFeature<AutoSubscribe>(),
            p => p.RegisterPublisherFor<DemoEvent>(typeof(PublishingEndpoint)));
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
                    ctx.Unsubscribed = true;
                }
            });
        });
    }

    public class DemoEvent : IEvent
    {
    }
}