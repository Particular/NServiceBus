namespace NServiceBus.TransportTests;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Time.Testing;
using NUnit.Framework;
using static InMemoryBrokerSimulationTestHelper;

[TestFixture]
public class When_simulating_send_delay
{
    [Test]
    public async Task Should_wait_for_simulated_time_to_advance()
    {
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2026, 03, 28, 12, 0, 0, TimeSpan.Zero));
        await using var broker = new InMemoryBroker(new InMemoryBrokerOptions
        {
            TimeProvider = fakeTime,
            Send = { RateLimit = new InMemoryRateLimitOptions { PermitLimit = 1, Window = TimeSpan.FromSeconds(5) } }
        });

        var dispatcher = await CreateDispatcher(broker, CancellationToken.None);
        await Dispatch(dispatcher, "msg-1", "queue", CancellationToken.None);

        var secondDispatch = Dispatch(dispatcher, "msg-2", "queue", CancellationToken.None);
        Assert.That(secondDispatch.IsCompleted, Is.False);

        fakeTime.Advance(TimeSpan.FromSeconds(5));
        await AsyncSpinWait.Until(() => secondDispatch.IsCompleted, maxIterations: 100);

        Assert.That(secondDispatch.IsCompleted, Is.True);
        await secondDispatch;

        Assert.That(broker.GetOrCreateQueue("queue").Count, Is.EqualTo(2));
    }
}

[TestFixture]
public class When_simulating_send_delay_with_queue_override
{
    [Test]
    public async Task Should_use_queue_operation_settings_over_broker_defaults()
    {
        var simulatedTime = new FakeTimeProvider(new DateTimeOffset(2026, 03, 28, 12, 0, 0, TimeSpan.Zero));
        var options = new InMemoryBrokerOptions
        {
            TimeProvider = simulatedTime,
            Send = { RateLimit = new InMemoryRateLimitOptions { PermitLimit = 1, Window = TimeSpan.FromSeconds(30) } }
        };
        options.ForQueue("queue").Send.RateLimit = new InMemoryRateLimitOptions { PermitLimit = 2, Window = TimeSpan.FromSeconds(30) };

        await using var broker = new InMemoryBroker(options);
        var dispatcher = await CreateDispatcher(broker);

        await Dispatch(dispatcher, "msg-1", "queue");
        await Dispatch(dispatcher, "msg-2", "queue");
        var thirdDispatch = Dispatch(dispatcher, "msg-3", "queue");

        await AsyncSpinWait.Until(() => broker.GetOrCreateQueue("queue").Count == 2, maxIterations: 20);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(thirdDispatch.IsCompleted, Is.False);
            Assert.That(broker.GetOrCreateQueue("queue").Count, Is.EqualTo(2));
        }

        simulatedTime.Advance(TimeSpan.FromSeconds(30));
        await AsyncSpinWait.Until(() => thirdDispatch.IsCompleted, maxIterations: 100);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(thirdDispatch.IsCompleted, Is.True);
            Assert.That(broker.GetOrCreateQueue("queue").Count, Is.EqualTo(3));
        }
    }
}

[TestFixture]
public class When_simulating_send_delay_with_queue_time_provider_override
{
    [Test]
    public async Task Should_use_queue_operation_time_provider_over_broker_time_provider()
    {
        var brokerTime = new FakeTimeProvider(new DateTimeOffset(2026, 03, 28, 12, 0, 0, TimeSpan.Zero));
        var queueTime = new FakeTimeProvider(new DateTimeOffset(2026, 03, 28, 12, 0, 0, TimeSpan.Zero));
        var options = new InMemoryBrokerOptions
        {
            TimeProvider = brokerTime,
            Send = { RateLimit = new InMemoryRateLimitOptions { PermitLimit = 1, Window = TimeSpan.FromSeconds(5) } }
        };
        options.ForQueue("queue").Send.TimeProvider = queueTime;

        await using var broker = new InMemoryBroker(options);
        var dispatcher = await CreateDispatcher(broker);

        await Dispatch(dispatcher, "msg-1", "queue");
        var secondDispatch = Dispatch(dispatcher, "msg-2", "queue");

        brokerTime.Advance(TimeSpan.FromSeconds(5));
        await AsyncSpinWait.Until(() => secondDispatch.IsCompleted, maxIterations: 20);
        Assert.That(secondDispatch.IsCompleted, Is.False);

        queueTime.Advance(TimeSpan.FromSeconds(5));
        await AsyncSpinWait.Until(() => secondDispatch.IsCompleted, maxIterations: 100);
        Assert.That(secondDispatch.IsCompleted, Is.True);
    }
}

[TestFixture]
public class When_simulating_send_reject
{
    [Test]
    public void Should_throw_immediately()
    {
        Assert.DoesNotThrowAsync(async () =>
        {
            await using var broker = new InMemoryBroker(new InMemoryBrokerOptions
            {
                Send = { Mode = InMemorySimulationMode.Reject, RateLimit = new InMemoryRateLimitOptions { PermitLimit = 1, Window = TimeSpan.FromMinutes(1) } }
            });

            var dispatcher = await CreateDispatcher(broker, CancellationToken.None);
            await Dispatch(dispatcher, "msg-1", "queue", CancellationToken.None);
        });

        Assert.ThrowsAsync<InMemorySimulationException>(async () =>
        {
            await using var broker = new InMemoryBroker(new InMemoryBrokerOptions
            {
                Send = { Mode = InMemorySimulationMode.Reject, RateLimit = new InMemoryRateLimitOptions { PermitLimit = 1, Window = TimeSpan.FromMinutes(1) } }
            });

            var dispatcher = await CreateDispatcher(broker, CancellationToken.None);
            await Dispatch(dispatcher, "msg-1", "queue", CancellationToken.None);
            await Dispatch(dispatcher, "msg-2", "queue", CancellationToken.None);
        });
    }
}
