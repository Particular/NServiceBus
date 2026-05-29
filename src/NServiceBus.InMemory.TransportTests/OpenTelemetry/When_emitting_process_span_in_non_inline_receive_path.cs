#nullable enable

namespace NServiceBus.TransportTests;

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Transport;
using static InMemoryBrokerSimulationTestHelper;

[TestFixture]
public class When_emitting_process_span_in_non_inline_receive_path
{
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