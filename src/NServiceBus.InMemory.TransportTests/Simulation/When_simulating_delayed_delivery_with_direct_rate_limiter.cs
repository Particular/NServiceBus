namespace NServiceBus.TransportTests;

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Time.Testing;
using NUnit.Framework;

[TestFixture]
public class When_simulating_delayed_delivery_with_direct_rate_limiter
{
    [Test]
    public async Task Should_use_configured_limiter()
    {
        var simulatedTime = new FakeTimeProvider(new DateTimeOffset(2026, 03, 28, 12, 0, 0, TimeSpan.Zero));
        await using var limiter = new CountingRateLimiter(permitCount: 1);
        await using var broker = new InMemoryBroker(new InMemoryBrokerOptions
        {
            TimeProvider = simulatedTime,
            DelayedDelivery =
            {
                Mode = InMemorySimulationMode.Reject,
                RateLimiter = limiter
            }
        });

        broker.EnqueueDelayed(InMemoryBrokerSimulationTestHelper.CreateEnvelope("msg-1", "queue", 1), simulatedTime.GetUtcNow());
        await broker.StartPump();

        var queue = broker.GetOrCreateQueue("queue");
        await AsyncSpinWait.Until(() => queue.Count == 1, maxIterations: 100);

        Assert.That(queue.Count, Is.EqualTo(1));
        Assert.That(limiter.AttemptAcquireCalls, Is.GreaterThanOrEqualTo(1));
    }
}