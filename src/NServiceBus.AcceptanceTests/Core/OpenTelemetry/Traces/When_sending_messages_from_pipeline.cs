namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry.Traces;

using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus.AcceptanceTesting;
using NUnit.Framework;

public class When_sending_messages_from_pipeline : OpenTelemetryAcceptanceTest
{
    [Test]
    public async Task Should_add_batch_dispatch_events()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<TestEndpoint>(b => b
                .When(s => s.SendLocal(new TriggerMessage())))
            .Run();

        var outgoingMessageActivities = NServiceBusActivityListener.CompletedActivities.GetReceiveMessageActivities();
        var sentMessage = outgoingMessageActivities.First();

        Assert.That(sentMessage.Events, Is.Not.Empty);
        var startDispatchingEvents = sentMessage.Events.Where(e => e.Name == "Start dispatching").ToArray();
        Assert.That(startDispatchingEvents.Length, Is.EqualTo(1), "should raise dispatch start event");
        Assert.That(startDispatchingEvents.Single().Tags.ToImmutableDictionary()["message-count"], Is.EqualTo(1));
        Assert.That(sentMessage.Events.Count(e => e.Name == "Finished dispatching"), Is.EqualTo(1), "should raise dispatch completed event");
    }

    class Context : ScenarioContext
    {
        public bool OutgoingMessageReceived { get; set; }
    }

    class TestEndpoint : EndpointConfigurationBuilder
    {
        public TestEndpoint() => EndpointSetup<DefaultServer>();

        class MessageHandler(Context testContext) : IHandleMessages<OutgoingMessage>, IHandleMessages<TriggerMessage>
        {
            public Task Handle(TriggerMessage message, IMessageHandlerContext context) => context.SendLocal(new OutgoingMessage());

            public Task Handle(OutgoingMessage message, IMessageHandlerContext context)
            {
                testContext.OutgoingMessageReceived = true;
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class TriggerMessage : IMessage;

    public class OutgoingMessage : IMessage;
}