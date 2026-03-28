namespace NServiceBus.TransportTests;

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Time.Testing;
using NUnit.Framework;
using static InMemoryBrokerSimulationTestHelper;

[TestFixture]
public class When_simulating_delayed_delivery_delay
{
    [Test]
    public async Task Should_wait_for_simulated_time_to_advance()
    {
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2026, 03, 28, 12, 0, 0, TimeSpan.Zero));
        await using var broker = new InMemoryBroker(new InMemoryBrokerOptions
        {
            TimeProvider = fakeTime,
            DelayedDelivery = { RateLimit = new InMemoryRateLimitOptions { PermitLimit = 1, Window = TimeSpan.FromSeconds(5) } }
        });

        broker.EnqueueDelayed(CreateEnvelope("msg-1", "queue", 1), fakeTime.GetUtcNow());
        broker.EnqueueDelayed(CreateEnvelope("msg-2", "queue", 2), fakeTime.GetUtcNow());
        await broker.StartPump();

        var queue = broker.GetOrCreateQueue("queue");
        await AsyncSpinWait.Until(() => queue.Count > 0, maxIterations: 100);
        Assert.That(queue.Count, Is.EqualTo(1));

        fakeTime.Advance(TimeSpan.FromSeconds(5));
        await AsyncSpinWait.Until(() => queue.Count >= 2, maxIterations: 100);

        Assert.That(queue.Count, Is.EqualTo(2));
    }
}

[TestFixture]
public class When_simulating_delayed_delivery_delay_with_queue_override
{
    [Test]
    public async Task Should_use_queue_operation_settings_over_broker_defaults()
    {
        var simulatedTime = new FakeTimeProvider(new DateTimeOffset(2026, 03, 28, 12, 0, 0, TimeSpan.Zero));
        var options = new InMemoryBrokerOptions
        {
            TimeProvider = simulatedTime,
            DelayedDelivery = { RateLimit = new InMemoryRateLimitOptions { PermitLimit = 1, Window = TimeSpan.FromSeconds(30) } }
        };
        options.ForQueue("queue").DelayedDelivery.RateLimit = new InMemoryRateLimitOptions { PermitLimit = 2, Window = TimeSpan.FromSeconds(30) };

        await using var broker = new InMemoryBroker(options);
        broker.EnqueueDelayed(CreateEnvelope("msg-1", "queue", 1), simulatedTime.GetUtcNow());
        broker.EnqueueDelayed(CreateEnvelope("msg-2", "queue", 2), simulatedTime.GetUtcNow());
        broker.EnqueueDelayed(CreateEnvelope("msg-3", "queue", 3), simulatedTime.GetUtcNow());
        await broker.StartPump();

        await AsyncSpinWait.Until(() => broker.GetOrCreateQueue("queue").Count == 2, maxIterations: 100);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(broker.GetOrCreateQueue("queue").Count, Is.EqualTo(2));
        }

        simulatedTime.Advance(TimeSpan.FromSeconds(30));
        await AsyncSpinWait.Until(() => broker.GetOrCreateQueue("queue").Count == 3, maxIterations: 100);
        Assert.That(broker.GetOrCreateQueue("queue").Count, Is.EqualTo(3));
    }
}

[TestFixture]
public class When_simulating_delayed_delivery_reject
{
    [Test]
    public async Task Should_retry_when_simulated_time_advances()
    {
        var simulatedTime = new FakeTimeProvider(new DateTimeOffset(2026, 03, 28, 12, 0, 0, TimeSpan.Zero));
        await using var broker = new InMemoryBroker(new InMemoryBrokerOptions
        {
            TimeProvider = simulatedTime,
            DelayedDelivery =
            {
                Mode = InMemorySimulationMode.Reject,
                RateLimit = new InMemoryRateLimitOptions { PermitLimit = 1, Window = TimeSpan.FromSeconds(5) }
            }
        });

        broker.EnqueueDelayed(CreateEnvelope("msg-1", "queue", 1), simulatedTime.GetUtcNow());
        broker.EnqueueDelayed(CreateEnvelope("msg-2", "queue", 2), simulatedTime.GetUtcNow());
        await broker.StartPump();

        var queue = broker.GetOrCreateQueue("queue");
        await AsyncSpinWait.Until(() => queue.Count == 1, maxIterations: 100);
        Assert.That(queue.Count, Is.EqualTo(1));

        simulatedTime.Advance(TimeSpan.FromSeconds(5));
        await AsyncSpinWait.Until(() => queue.Count == 2, maxIterations: 100);
        Assert.That(queue.Count, Is.EqualTo(2));
    }
}
