namespace NServiceBus.TransportTests;

using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Transport;

[TestFixture]
public class When_simulating_receive_with_direct_rate_limiter
{
    [Test]
    public async Task Should_use_configured_limiter()
    {
        await using var limiter = new CountingRateLimiter(permitCount: 1);
        await using var broker = new InMemoryBroker(new InMemoryBrokerOptions
        {
            Receive =
            {
                Mode = InMemorySimulationMode.Reject,
                RateLimiter = limiter
            }
        });

        var receiver = await InMemoryBrokerSimulationTestHelper.CreateReceiver(broker);
        var firstReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var receivedCount = 0;

        await receiver.Initialize(
            new PushRuntimeSettings(maxConcurrency: 1),
            (_, _) =>
            {
                Interlocked.Increment(ref receivedCount);
                firstReceived.TrySetResult();
                return Task.CompletedTask;
            },
            (_, _) => Task.FromResult(ErrorHandleResult.Handled));

        var queue = broker.GetOrCreateQueue("input");
        await queue.Enqueue(InMemoryBrokerSimulationTestHelper.CreateEnvelope("msg-1", "input", 1), CancellationToken.None);
        await queue.Enqueue(InMemoryBrokerSimulationTestHelper.CreateEnvelope("msg-2", "input", 2), CancellationToken.None);
        await receiver.StartReceive();

        await firstReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.That(Volatile.Read(ref receivedCount), Is.EqualTo(1));
        Assert.That(limiter.AttemptAcquireCalls, Is.GreaterThanOrEqualTo(1));

        await receiver.StopReceive();
    }
}