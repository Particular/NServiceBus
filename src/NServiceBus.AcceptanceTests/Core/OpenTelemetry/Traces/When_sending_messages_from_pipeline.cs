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
            .Done(c => c.OutgoingMessageReceived)
            .Run();

        var outgoingMessageActivities = NServicebusActivityListener.CompletedActivities.GetReceiveMessageActivities();
        var sentMessage = outgoingMessageActivities.First();

        Assert.IsNotEmpty(sentMessage.Events);
        var startDispatchingEvents = sentMessage.Events.Where(e => e.Name == "Start dispatching").ToArray();
        Assert.That(startDispatchingEvents.Length, Is.EqualTo(1), "should raise dispatch start event");
        Assert.That(startDispatchingEvents.Single().Tags.ToImmutableDictionary()["message-count"], Is.EqualTo(1));
        Assert.AreEqual(1, sentMessage.Events.Count(e => e.Name == "Finished dispatching"), "should raise dispatch completed event");
    }

    class Context : ScenarioContext
    {
        public bool OutgoingMessageReceived { get; set; }
    }

    class TestEndpoint : EndpointConfigurationBuilder
    {
        public TestEndpoint() => EndpointSetup<OpenTelemetryEnabledEndpoint>();

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