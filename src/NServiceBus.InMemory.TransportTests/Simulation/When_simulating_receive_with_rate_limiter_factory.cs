namespace NServiceBus.TransportTests;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Transport;

[TestFixture]
public class When_simulating_receive_with_rate_limiter_factory
{
    [Test]
    public async Task Should_create_and_reuse_limiter_per_operation_and_queue()
    {
        var factoryCalls = 0;
        var createdLimiters = new List<CountingRateLimiter>();
        await using var broker = new InMemoryBroker(new InMemoryBrokerOptions
        {
            Receive =
            {
                Mode = InMemorySimulationMode.Reject,
                RateLimiterFactory = _ =>
                {
                    factoryCalls++;
                    var limiter = new CountingRateLimiter(permitCount: 1);
                    createdLimiters.Add(limiter);
                    return limiter;
                }
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
        Assert.That(factoryCalls, Is.EqualTo(1));

        await receiver.StopReceive();

        foreach (var l in createdLimiters)
        {
            await l.DisposeAsync();
        }
    }
}