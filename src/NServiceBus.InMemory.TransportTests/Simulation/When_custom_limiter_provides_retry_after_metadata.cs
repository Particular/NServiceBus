namespace NServiceBus.TransportTests;

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Time.Testing;
using NUnit.Framework;

[TestFixture]
public class When_custom_limiter_provides_retry_after_metadata
{
    [Test]
    public async Task Should_delay_broker_retry_until_retry_after_elapses()
    {
        var simulatedTime = new FakeTimeProvider(new DateTimeOffset(2026, 03, 28, 12, 0, 0, TimeSpan.Zero));
        await using var limiter = new ScriptedRateLimiter(
        [
            ScriptedRateLimiterStep.Acquired(),
            ScriptedRateLimiterStep.Rejected(TimeSpan.FromSeconds(30)),
            ScriptedRateLimiterStep.Acquired()
        ]);

        await using var broker = new InMemoryBroker(new InMemoryBrokerOptions
        {
            TimeProvider = simulatedTime,
            DelayedDelivery =
            {
                Mode = InMemorySimulationMode.Delay,
                RateLimiter = limiter
            }
        });

        broker.EnqueueDelayed(InMemoryBrokerSimulationTestHelper.CreateEnvelope("msg-1", "queue", 1), simulatedTime.GetUtcNow());
        broker.EnqueueDelayed(InMemoryBrokerSimulationTestHelper.CreateEnvelope("msg-2", "queue", 2), simulatedTime.GetUtcNow());
        await broker.StartPump();

        var queue = broker.GetOrCreateQueue("queue");
        await AsyncSpinWait.Until(() => queue.Count == 1, maxIterations: 100);
        Assert.That(queue.Count, Is.EqualTo(1));

        simulatedTime.Advance(TimeSpan.FromSeconds(29));
        await AsyncSpinWait.Until(() => queue.Count > 1, maxIterations: 20);
        Assert.That(queue.Count, Is.EqualTo(1));

        simulatedTime.Advance(TimeSpan.FromSeconds(1));
        await AsyncSpinWait.Until(() => queue.Count == 2, maxIterations: 100);
        Assert.That(queue.Count, Is.EqualTo(2));
    }
}