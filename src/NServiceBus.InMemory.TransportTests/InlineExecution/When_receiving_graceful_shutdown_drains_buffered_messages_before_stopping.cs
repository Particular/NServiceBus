#nullable enable

namespace NServiceBus.TransportTests;

using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Transport;

[TestFixture]
public class When_receiving_graceful_shutdown_drains_buffered_messages_before_stopping
{
    [Test]
    public async Task Run()
    {
        await using var broker = new InMemoryBroker();
        var infrastructure = await CreateInfrastructure(broker, ["input"]);
        var dispatcher = infrastructure.Dispatcher;
        var receiver = infrastructure.Receivers["receiver-0"];
        var firstHandlerStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirstHandler = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondMessageHandled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var handledMessages = 0;

        await receiver.Initialize(
            new PushRuntimeSettings(maxConcurrency: 1),
            async (_, cancellationToken) =>
            {
                var current = Interlocked.Increment(ref handledMessages);
                if (current == 1)
                {
                    firstHandlerStarted.TrySetResult();
                    await releaseFirstHandler.Task.WaitAsync(cancellationToken);
                    return;
                }

                secondMessageHandled.TrySetResult();
            },
            (_, _) => Task.FromResult(ErrorHandleResult.Handled),
            CancellationToken.None);

        await receiver.StartReceive();

        var firstDispatch = dispatcher.Dispatch(new TransportOperations(CreateUnicast("input")), new TransportTransaction());
        var secondDispatch = dispatcher.Dispatch(new TransportOperations(CreateUnicast("input")), new TransportTransaction());

        await firstHandlerStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        var stopTask = receiver.StopReceive();

        Assert.That(stopTask.IsCompleted, Is.False);

        releaseFirstHandler.TrySetResult();

        await secondMessageHandled.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await stopTask.WaitAsync(TimeSpan.FromSeconds(5));
        await firstDispatch.WaitAsync(TimeSpan.FromSeconds(5));
        await secondDispatch.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Multiple(() =>
        {
            Assert.That(handledMessages, Is.EqualTo(2));
            Assert.That(broker.GetOrCreateQueue("input").Count, Is.EqualTo(0));
        });
    }
}
