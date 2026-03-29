namespace NServiceBus.TransportTests;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Time.Testing;
using NServiceBus.Transport;
using NUnit.Framework;
using static InMemoryBrokerSimulationTestHelper;

[TestFixture]
public class When_queue_level_default_rate_limit_takes_precedence_over_broker_default
{
    [Test]
    public async Task Should_apply_queue_default_for_send_over_broker_default()
    {
        var simulatedTime = new FakeTimeProvider(new DateTimeOffset(2026, 03, 28, 12, 0, 0, TimeSpan.Zero));
        var options = new InMemoryBrokerOptions
        {
            TimeProvider = simulatedTime,
            Send = { RateLimit = new InMemoryRateLimitOptions { PermitLimit = 1, Window = TimeSpan.FromSeconds(30) } }
        };
        // Queue-level default (not operation-specific) - should take precedence over broker Send default
        options.ForQueue("queue").Default.RateLimit = new InMemoryRateLimitOptions { PermitLimit = 2, Window = TimeSpan.FromSeconds(30) };

        await using var broker = new InMemoryBroker(options);
        var dispatcher = await CreateDispatcher(broker);

        // With queue default of 2 permits, we should be able to send 2 messages immediately
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
public class When_queue_operation_override_takes_precedence_over_queue_default
{
    [Test]
    public async Task Should_apply_send_operation_override_over_queue_default()
    {
        var simulatedTime = new FakeTimeProvider(new DateTimeOffset(2026, 03, 28, 12, 0, 0, TimeSpan.Zero));
        var options = new InMemoryBrokerOptions
        {
            TimeProvider = simulatedTime
        };
        // Queue-level default
        options.ForQueue("queue").Default.RateLimit = new InMemoryRateLimitOptions { PermitLimit = 5, Window = TimeSpan.FromSeconds(30) };
        // Queue + Send operation override - should take precedence
        options.ForQueue("queue").Send.RateLimit = new InMemoryRateLimitOptions { PermitLimit = 1, Window = TimeSpan.FromSeconds(30) };

        await using var broker = new InMemoryBroker(options);
        var dispatcher = await CreateDispatcher(broker);

        // Should only allow 1 due to queue+operation override
        await Dispatch(dispatcher, "msg-1", "queue");
        var secondDispatch = Dispatch(dispatcher, "msg-2", "queue");

        await AsyncSpinWait.Until(() => broker.GetOrCreateQueue("queue").Count == 1, maxIterations: 20);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(secondDispatch.IsCompleted, Is.False);
            Assert.That(broker.GetOrCreateQueue("queue").Count, Is.EqualTo(1));
        }

        simulatedTime.Advance(TimeSpan.FromSeconds(30));
        await AsyncSpinWait.Until(() => secondDispatch.IsCompleted, maxIterations: 100);
        Assert.That(broker.GetOrCreateQueue("queue").Count, Is.EqualTo(2));
    }
}

[TestFixture]
public class When_operation_level_override_takes_precedence_over_broker_default
{
    [Test]
    public async Task Should_apply_receive_operation_override_over_broker_default()
    {
        var simulatedTime = new FakeTimeProvider(new DateTimeOffset(2026, 03, 28, 12, 0, 0, TimeSpan.Zero));
        var options = new InMemoryBrokerOptions
        {
            TimeProvider = simulatedTime,
            Default = { RateLimit = new InMemoryRateLimitOptions { PermitLimit = 5, Window = TimeSpan.FromSeconds(30) } },
            Receive = { RateLimit = new InMemoryRateLimitOptions { PermitLimit = 1, Window = TimeSpan.FromSeconds(30) } }
        };

        await using var broker = new InMemoryBroker(options);
        var receiver = await CreateReceiver(broker);
        var firstReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var receivedCount = 0;

        await receiver.Initialize(
            new PushRuntimeSettings(maxConcurrency: 1),
            (_, _) =>
            {
                var currentCount = Interlocked.Increment(ref receivedCount);
                if (currentCount == 1)
                {
                    firstReceived.TrySetResult();
                }
                else if (currentCount == 2)
                {
                    secondReceived.TrySetResult();
                }
                return Task.CompletedTask;
            },
            (_, _) => Task.FromResult(ErrorHandleResult.Handled));

        var queue = broker.GetOrCreateQueue("input");
        await queue.Enqueue(CreateEnvelope("msg-1", "input", 1), CancellationToken.None);
        await queue.Enqueue(CreateEnvelope("msg-2", "input", 2), CancellationToken.None);
        await receiver.StartReceive();

        await firstReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.That(Volatile.Read(ref receivedCount), Is.EqualTo(1));
        Assert.That(secondReceived.Task.IsCompleted, Is.False);

        simulatedTime.Advance(TimeSpan.FromSeconds(30));
        await secondReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.That(Volatile.Read(ref receivedCount), Is.EqualTo(2));

        await receiver.StopReceive();
    }
}

[TestFixture]
public class When_mode_precedence_is_respected_for_send_configuration
{
    [Test]
    public void Should_apply_queue_operation_mode_over_broker_mode()
    {
        var options = new InMemoryBrokerOptions
        {
            Send = { Mode = InMemorySimulationMode.Delay }
        };
        // Queue + operation override should take precedence
        options.ForQueue("queue").Send.Mode = InMemorySimulationMode.Reject;

        Assert.DoesNotThrow(() => new InMemoryBroker(options));
    }
}

[TestFixture]
public class When_mode_precedence_is_respected_for_send_behavior
{
    [Test]
    public async Task Should_use_queue_operation_mode_reject_for_send()
    {
        var options = new InMemoryBrokerOptions
        {
            Send = { Mode = InMemorySimulationMode.Delay }
        };
        options.ForQueue("queue").Send.Mode = InMemorySimulationMode.Reject;
        options.ForQueue("queue").Send.RateLimit = new InMemoryRateLimitOptions { PermitLimit = 1, Window = TimeSpan.FromSeconds(30) };

        await using var broker = new InMemoryBroker(options);
        var dispatcher = await CreateDispatcher(broker);

        await Dispatch(dispatcher, "msg-1", "queue");
        Assert.ThrowsAsync<InMemorySimulationException>(async () => await Dispatch(dispatcher, "msg-2", "queue"));
    }
}

[TestFixture]
public class When_mode_precedence_is_respected_for_receive
{
    [Test]
    public async Task Should_use_queue_operation_mode_reject_for_receive()
    {
        var options = new InMemoryBrokerOptions
        {
            Receive = { Mode = InMemorySimulationMode.Delay }
        };
        options.ForQueue("input").Receive.Mode = InMemorySimulationMode.Reject;
        options.ForQueue("input").Receive.RateLimit = new InMemoryRateLimitOptions { PermitLimit = 1, Window = TimeSpan.FromSeconds(30) };

        await using var broker = new InMemoryBroker(options);
        var receiver = await CreateReceiver(broker);
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
        await queue.Enqueue(CreateEnvelope("msg-1", "input", 1), CancellationToken.None);
        await queue.Enqueue(CreateEnvelope("msg-2", "input", 2), CancellationToken.None);
        await receiver.StartReceive();

        await firstReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.That(Volatile.Read(ref receivedCount), Is.EqualTo(1));

        await receiver.StopReceive();
    }
}

[TestFixture]
public class When_mode_precedence_is_respected_for_delayed_delivery
{
    [Test]
    public async Task Should_use_queue_operation_mode_reject_for_delayed_delivery()
    {
        var options = new InMemoryBrokerOptions
        {
            DelayedDelivery = { Mode = InMemorySimulationMode.Delay }
        };
        options.ForQueue("queue").DelayedDelivery.Mode = InMemorySimulationMode.Reject;
        options.ForQueue("queue").DelayedDelivery.RateLimit = new InMemoryRateLimitOptions { PermitLimit = 1, Window = TimeSpan.FromSeconds(30) };

        await using var broker = new InMemoryBroker(options);
        broker.EnqueueDelayed(CreateEnvelope("msg-1", "queue", 1), DateTimeOffset.UtcNow);
        broker.EnqueueDelayed(CreateEnvelope("msg-2", "queue", 2), DateTimeOffset.UtcNow);
        await broker.StartPump();

        var queue = broker.GetOrCreateQueue("queue");
        await AsyncSpinWait.Until(() => queue.Count == 1, maxIterations: 100);
        Assert.That(queue.Count, Is.EqualTo(1));
    }
}

[TestFixture]
public class When_time_provider_precedence_uses_broker_default
{
    [Test]
    public async Task Should_use_broker_default_time_provider_when_no_overrides()
    {
        var brokerTime = new FakeTimeProvider(new DateTimeOffset(2026, 03, 28, 12, 0, 0, TimeSpan.Zero));
        var options = new InMemoryBrokerOptions
        {
            TimeProvider = brokerTime,
            Send = { RateLimit = new InMemoryRateLimitOptions { PermitLimit = 1, Window = TimeSpan.FromSeconds(5) } }
        };

        await using var broker = new InMemoryBroker(options);
        var dispatcher = await CreateDispatcher(broker);

        await Dispatch(dispatcher, "msg-1", "queue");
        var secondDispatch = Dispatch(dispatcher, "msg-2", "queue");

        brokerTime.Advance(TimeSpan.FromSeconds(5));
        await AsyncSpinWait.Until(() => secondDispatch.IsCompleted, maxIterations: 100);
        Assert.That(secondDispatch.IsCompleted, Is.True);
    }
}

[TestFixture]
public class When_time_provider_precedence_uses_operation_override
{
    [Test]
    public async Task Should_use_operation_time_provider_over_broker_default()
    {
        var brokerTime = new FakeTimeProvider(new DateTimeOffset(2026, 03, 28, 12, 0, 0, TimeSpan.Zero));
        var sendTime = new FakeTimeProvider(new DateTimeOffset(2026, 03, 28, 12, 0, 0, TimeSpan.Zero));
        var options = new InMemoryBrokerOptions
        {
            TimeProvider = brokerTime,
            Send =
            {
                TimeProvider = sendTime,
                RateLimit = new InMemoryRateLimitOptions { PermitLimit = 1, Window = TimeSpan.FromSeconds(5) }
            }
        };

        await using var broker = new InMemoryBroker(options);
        var dispatcher = await CreateDispatcher(broker);

        await Dispatch(dispatcher, "msg-1", "queue");
        var secondDispatch = Dispatch(dispatcher, "msg-2", "queue");

        brokerTime.Advance(TimeSpan.FromSeconds(5));
        await AsyncSpinWait.Until(() => secondDispatch.IsCompleted, maxIterations: 20);
        Assert.That(secondDispatch.IsCompleted, Is.False);

        sendTime.Advance(TimeSpan.FromSeconds(5));
        await AsyncSpinWait.Until(() => secondDispatch.IsCompleted, maxIterations: 100);
        Assert.That(secondDispatch.IsCompleted, Is.True);
    }
}

[TestFixture]
public class When_full_precedence_chain_is_validated_for_send
{
    [Test]
    public async Task Should_respect_full_precedence_hierarchy()
    {
        // Precedence chain (highest to lowest):
        // 1. queue + operation override
        // 2. queue-level default
        // 3. operation-level override
        // 4. broker-wide default
        var simulatedTime = new FakeTimeProvider(new DateTimeOffset(2026, 03, 28, 12, 0, 0, TimeSpan.Zero));
        var options = new InMemoryBrokerOptions
        {
            TimeProvider = simulatedTime,
            // Level 4: broker-wide default
            Send = { RateLimit = new InMemoryRateLimitOptions { PermitLimit = 10, Window = TimeSpan.FromSeconds(30) } },
            Default = { RateLimit = new InMemoryRateLimitOptions { PermitLimit = 20, Window = TimeSpan.FromSeconds(30) } }
        };

        // Level 3: queue-level default (should override broker Send default)
        options.ForQueue("queue").Default.RateLimit = new InMemoryRateLimitOptions { PermitLimit = 5, Window = TimeSpan.FromSeconds(30) };

        // Level 1: queue + operation override (should take highest precedence)
        options.ForQueue("queue").Send.RateLimit = new InMemoryRateLimitOptions { PermitLimit = 1, Window = TimeSpan.FromSeconds(30) };

        await using var broker = new InMemoryBroker(options);
        var dispatcher = await CreateDispatcher(broker);

        // Should only allow 1 due to queue+operation override
        await Dispatch(dispatcher, "msg-1", "queue");
        var secondDispatch = Dispatch(dispatcher, "msg-2", "queue");

        await AsyncSpinWait.Until(() => broker.GetOrCreateQueue("queue").Count == 1, maxIterations: 20);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(secondDispatch.IsCompleted, Is.False);
            Assert.That(broker.GetOrCreateQueue("queue").Count, Is.EqualTo(1));
        }

        simulatedTime.Advance(TimeSpan.FromSeconds(30));
        await AsyncSpinWait.Until(() => secondDispatch.IsCompleted, maxIterations: 100);
        Assert.That(broker.GetOrCreateQueue("queue").Count, Is.EqualTo(2));
    }
}
