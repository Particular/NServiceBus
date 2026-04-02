#nullable enable

namespace NServiceBus.TransportTests;

using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Transport;

[TestFixture]
public class When_receiving_graceful_shutdown_preserves_delayed_scopes_for_restart
{
    [Test]
    public async Task Run()
    {
        var fakeTime = CreateFakeTimeProvider();
        await using var broker = new InMemoryBroker(new InMemoryBrokerOptions
        {
            TimeProvider = fakeTime
        });
        var infrastructure = await CreateInfrastructure(broker, ["input"]);
        var dispatcher = infrastructure.Dispatcher;
        var receiver = infrastructure.Receivers["receiver-0"];
        var handled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await receiver.Initialize(
            new PushRuntimeSettings(maxConcurrency: 1),
            (_, _) =>
            {
                handled.TrySetResult();
                return Task.CompletedTask;
            },
            (_, _) => Task.FromResult(ErrorHandleResult.Handled),
            CancellationToken.None);

        await receiver.StartReceive();

        var rootTask = dispatcher.Dispatch(
            new TransportOperations(CreateUnicast("input", delay: TimeSpan.FromMinutes(1))),
            new TransportTransaction());

        await receiver.StopReceive().WaitAsync(TimeSpan.FromSeconds(5));

        Assert.That(rootTask.IsCompleted, Is.False);

        fakeTime.Advance(TimeSpan.FromMinutes(1));
        await AsyncSpinWait.Until(() => broker.GetOrCreateQueue("input").Count == 1);

        Assert.That(rootTask.IsCompleted, Is.False);

        await receiver.StartReceive();

        await handled.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await rootTask.WaitAsync(TimeSpan.FromSeconds(5));

        await receiver.StopReceive();
    }
}
