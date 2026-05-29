#nullable enable

namespace NServiceBus.TransportTests;

using System;
using System.Linq;
using System.Threading.Tasks;
using DelayedDelivery;
using NUnit.Framework;
using Routing;
using Transport;
using static InMemoryBrokerSimulationTestHelper;

[TestFixture]
public class When_emitting_schedule_span_for_delayed_delivery
{
    [Test]
    public async Task Should_create_schedule_span_for_delayed_delivery()
    {
        await using var broker = new InMemoryBroker();
        using var listener = new TestingActivityListener(InMemoryTransportTracing.ActivitySourceName);
        var dispatcher = await CreateDispatcher(broker);

        var message = new OutgoingMessage("msg-2", new() { [Headers.ConversationId] = "conversation-id" }, new byte[] { 1 });
        var properties = new DispatchProperties
        {
            DelayDeliveryWith = new DelayDeliveryWith(TimeSpan.FromSeconds(5))
        };

        await dispatcher.Dispatch(new TransportOperations(new TransportOperation(message, new UnicastAddressTag("queue"), properties)), new TransportTransaction());

        var scheduleActivity = listener.CompletedFrom(InMemoryTransportTracing.ActivitySourceName).Single(activity => activity.OperationName == InMemoryTransportTracing.ScheduleActivityName);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(scheduleActivity.DisplayName, Is.EqualTo("schedule"));
            Assert.That(scheduleActivity.GetTagItem("messaging.destination.name"), Is.EqualTo("queue"));
            Assert.That(scheduleActivity.GetTagItem("messaging.operation.name"), Is.EqualTo("schedule"));
            Assert.That(scheduleActivity.GetTagItem("messaging.operation.type"), Is.EqualTo("send"));
            Assert.That(scheduleActivity.GetTagItem("messaging.message.conversation_id"), Is.EqualTo("conversation-id"));
            Assert.That(scheduleActivity.Events.Any(e => e.Name == "inmemory.scheduled"), Is.True);
        }
    }
}