namespace NServiceBus.TransportTests;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Time.Testing;
using NUnit.Framework;

[TestFixture]
public class When_simulating_delayed_delivery_with_rate_limiter_factory
{
    [Test]
    public async Task Should_create_and_reuse_limiter_per_operation_and_queue()
    {
        var simulatedTime = new FakeTimeProvider(new DateTimeOffset(2026, 03, 28, 12, 0, 0, TimeSpan.Zero));
        var factoryCalls = 0;
        var createdLimiters = new List<CountingRateLimiter>();
        await using var broker = new InMemoryBroker(new InMemoryBrokerOptions
        {
            TimeProvider = simulatedTime,
            DelayedDelivery =
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

        broker.EnqueueDelayed(InMemoryBrokerSimulationTestHelper.CreateEnvelope("msg-1", "queue", 1), simulatedTime.GetUtcNow());
        broker.EnqueueDelayed(InMemoryBrokerSimulationTestHelper.CreateEnvelope("msg-2", "other-queue", 2), simulatedTime.GetUtcNow());
        await broker.StartPump();

        var queue1 = broker.GetOrCreateQueue("queue");
        var queue2 = broker.GetOrCreateQueue("other-queue");
        await AsyncSpinWait.Until(() => queue1.Count == 1 || queue2.Count == 1, maxIterations: 100);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(factoryCalls, Is.EqualTo(2));
            Assert.That(createdLimiters.Count, Is.EqualTo(2));
        }

        foreach (var l in createdLimiters)
        {
            await l.DisposeAsync();
        }
    }
}