#nullable enable

namespace NServiceBus.TransportTests;

using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Transport;

[TestFixture]
public class When_receiving_hard_shutdown_re_registers_inflight_scope
{
    [Test]
    public async Task Run()
    {
        await using var broker = new InMemoryBroker();
        var infrastructure = await CreateInfrastructure(broker, ["input"]);
        var dispatcher = infrastructure.Dispatcher;
        var receiver = infrastructure.Receivers["receiver-0"];
        var processingStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await receiver.Initialize(
            new PushRuntimeSettings(maxConcurrency: 1),
            async (_, cancellationToken) =>
            {
                processingStarted.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            },
            (_, _) => Task.FromResult(ErrorHandleResult.Handled),
            CancellationToken.None);

        await receiver.StartReceive();

        var rootTask = dispatcher.Dispatch(new TransportOperations(CreateUnicast("input")), new TransportTransaction());

        await processingStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        using var stopCts = new CancellationTokenSource();
        stopCts.Cancel();
        await receiver.StopReceive(stopCts.Token).WaitAsync(TimeSpan.FromSeconds(5));

        var exception = await CatchException(rootTask);
        Assert.That(exception, Is.InstanceOf<OperationCanceledException>());

        await AsyncSpinWait.Until(() => broker.GetOrCreateQueue("input").Count == 1);
        Assert.That(broker.DrainQueue("input"), Is.EqualTo(1));
    }
}
