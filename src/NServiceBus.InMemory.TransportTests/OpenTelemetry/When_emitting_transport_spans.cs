#nullable enable

namespace NServiceBus.TransportTests;

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DelayedDelivery;
using NUnit.Framework;
using Routing;
using Transport;
using static InMemoryBrokerSimulationTestHelper;

[TestFixture]
public class When_emitting_transport_spans
{
    [Test]
    public async Task Should_create_send_span_and_propagate_its_context()
    {
        await using var broker = new InMemoryBroker();
        using var listener = new TestingActivityListener(InMemoryTransportTracing.ActivitySourceName);
        var dispatcher = await CreateDispatcher(broker);

        await Dispatch(dispatcher, "msg-1", "queue");

        Assert.That(broker.GetOrCreateQueue("queue").TryPeek(out var envelope), Is.True);

        var sendActivity = listener.CompletedFrom(InMemoryTransportTracing.ActivitySourceName).Single(activity => activity.OperationName == InMemoryTransportTracing.SendActivityName);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sendActivity.DisplayName, Is.EqualTo("send"));
            Assert.That(sendActivity.Status, Is.EqualTo(ActivityStatusCode.Ok));
            Assert.That(sendActivity.GetTagItem("messaging.system"), Is.EqualTo("inmemory"));
            Assert.That(sendActivity.GetTagItem("messaging.destination.name"), Is.EqualTo("queue"));
            Assert.That(sendActivity.GetTagItem("messaging.operation.name"), Is.EqualTo("send"));
            Assert.That(sendActivity.GetTagItem("messaging.operation.type"), Is.EqualTo("send"));
            Assert.That(sendActivity.GetTagItem("messaging.message.id"), Is.EqualTo(envelope!.MessageId));
            Assert.That(sendActivity.Events.Any(e => e.Name == "inmemory.enqueued"), Is.True);
            Assert.That(envelope.Headers[Headers.DiagnosticsTraceParent], Is.EqualTo(sendActivity.Id));
        }
    }

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

    [Test]
    public async Task Should_not_change_headers_when_no_transport_listener_is_registered()
    {
        await using var broker = new InMemoryBroker();
        var dispatcher = await CreateDispatcher(broker);

        await Dispatch(dispatcher, "msg-3", "queue");

        Assert.That(broker.GetOrCreateQueue("queue").TryPeek(out var envelope), Is.True);
        Assert.That(envelope!.Headers.ContainsKey(Headers.DiagnosticsTraceParent), Is.False);
    }

    [Test]
    public async Task Should_create_process_span_in_the_non_inline_receive_path()
    {
        await using var broker = new InMemoryBroker();
        using var listener = new TestingActivityListener(InMemoryTransportTracing.ActivitySourceName);
        var dispatcher = await CreateDispatcher(broker);
        var receiver = await CreateReceiver(broker);
        var transportActivitySeenByHandler = new TaskCompletionSource<Activity?>(TaskCreationOptions.RunContinuationsAsynchronously);

        await receiver.Initialize(
            new PushRuntimeSettings(maxConcurrency: 1),
            (messageContext, _) =>
            {
                messageContext.Extensions.TryGet<Activity>(out var transportActivity);
                transportActivitySeenByHandler.TrySetResult(transportActivity);
                return Task.CompletedTask;
            },
            (_, _) => Task.FromResult(ErrorHandleResult.Handled));

        await receiver.StartReceive();
        await Dispatch(dispatcher, "msg-4", "input");

        var observedTransportActivity = await transportActivitySeenByHandler.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await receiver.StopReceive();

        var transportActivities = listener.CompletedFrom(InMemoryTransportTracing.ActivitySourceName);
        var sendActivity = transportActivities.Single(activity => activity.OperationName == InMemoryTransportTracing.SendActivityName);
        var processActivity = transportActivities.Single(activity => activity.OperationName == InMemoryTransportTracing.ProcessActivityName);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(observedTransportActivity, Is.Not.Null);
            Assert.That(observedTransportActivity, Is.SameAs(processActivity));
            Assert.That(processActivity.ParentId, Is.EqualTo(sendActivity.Id));
            Assert.That(processActivity.GetTagItem("messaging.destination.name"), Is.EqualTo("input"));
            Assert.That(processActivity.GetTagItem("messaging.operation.name"), Is.EqualTo("process"));
            Assert.That(processActivity.GetTagItem("messaging.operation.type"), Is.EqualTo("process"));
            Assert.That(processActivity.Events.Any(e => e.Name == "inmemory.handoff"), Is.True);
        }
    }
}
