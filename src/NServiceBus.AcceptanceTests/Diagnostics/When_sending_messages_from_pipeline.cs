namespace NServiceBus.AcceptanceTests.Diagnostics;

using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

[NonParallelizable] // Ensure only activities for the current test are captured
public class When_sending_messages_from_pipeline : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_add_batch_dispatch_events()
    {
        using var activityListener = TestingActivityListener.SetupNServiceBusDiagnosticListener();
        var context = await Scenario.Define<Context>()
            .WithEndpoint<TestEndpoint>(b => b
                .When(s => s.SendLocal(new TriggerMessage())))
            .Done(c => c.OutgoingMessageReceived)
            .Run();

        Assert.AreEqual(activityListener.CompletedActivities.Count, activityListener.StartedActivities.Count, "all activities should be completed");

        var outgoingMessageActivities = activityListener.CompletedActivities.GetIncomingActivities();
        var sentMessage = outgoingMessageActivities.First();

        Assert.IsNotEmpty(sentMessage.Events);
        var startDispatchingEvents = sentMessage.Events.Where(e => e.Name == "Start dispatching").ToArray();
        Assert.AreEqual(1, startDispatchingEvents.Length, "should raise dispatch start event");
        Assert.AreEqual(1, startDispatchingEvents.Single().Tags.ToImmutableDictionary()["message-count"]);
        Assert.AreEqual(1, sentMessage.Events.Count(e => e.Name == "Finished dispatching"), "should raise dispatch completed event");
    }

    class Context : ScenarioContext
    {
        public bool OutgoingMessageReceived { get; set; }
    }

    class TestEndpoint : EndpointConfigurationBuilder
    {
        public TestEndpoint() => EndpointSetup<DefaultServer>();

        class MessageHandler : IHandleMessages<OutgoingMessage>, IHandleMessages<TriggerMessage>
        {
            readonly Context testContext;

            public MessageHandler(Context testContext) => this.testContext = testContext;

            public Task Handle(TriggerMessage message, IMessageHandlerContext context) => context.SendLocal(new OutgoingMessage());

            public Task Handle(OutgoingMessage message, IMessageHandlerContext context)
            {
                testContext.OutgoingMessageReceived = true;
                return Task.CompletedTask;
            }
        }
    }

    public class TriggerMessage : IMessage
    {

    }

    public class OutgoingMessage : IMessage
    {
    }
}