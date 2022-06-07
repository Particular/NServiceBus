namespace NServiceBus.AcceptanceTests.Diagnostics;

using System.Linq;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

//TODO: we might not need this test if we verify that we copy NSB headers to tags because then we can just use the Headers.ControlMessage value instead
[NonParallelizable] // Ensure only activities for the current test are captured
public class When_using_control_messages : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_mark_control_messages()
    {
        Requires.MessageDrivenPubSub();

        using var activityListener = TestingActivityListener.SetupNServiceBusDiagnosticListener();

        var context = await Scenario.Define<Context>()
            .WithEndpoint<Publisher>()
            .WithEndpoint<Subscriber>(e => e.When(s => s.Subscribe<MyEvent>()))
            .Done(c => c.SubscriptionMessageReceived)
            .Run();

        //TODO subscribe/unsubscribe don't create activities, should they?
        Assert.AreEqual(1, activityListener.CompletedActivities.Count);
        Assert.AreEqual(bool.TrueString, activityListener.CompletedActivities.Single().GetTagItem("nservicebus.control_message")); // value is a string
    }

    class Context : ScenarioContext
    {
        public bool SubscriptionMessageReceived { get; set; }
    }

    class Publisher : EndpointConfigurationBuilder
    {
        public Publisher() => EndpointSetup<DefaultServer>((endpointConfiguration) =>
            endpointConfiguration.OnEndpointSubscribed(
                (SubscriptionEvent e, Context c) =>
                {
                    if (e.MessageType.Contains(typeof(MyEvent).FullName))
                    {
                        c.SubscriptionMessageReceived = true;
                    }
                }));
    }

    class Subscriber : EndpointConfigurationBuilder
    {
        public Subscriber() => EndpointSetup<DefaultServer>(endpointConfiguration => { }, metadata => metadata.RegisterPublisherFor<MyEvent>(typeof(Publisher)));
    }

    public class MyEvent : IEvent
    {
    }
}