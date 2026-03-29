namespace NServiceBus.TransportTests;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using Microsoft.Extensions.Time.Testing;
using NServiceBus.Transport;
using NUnit.Framework;
using static InMemoryBrokerSimulationTestHelper;

[TestFixture]
public class When_simulating_send_with_direct_rate_limiter
{
    [Test]
    public async Task Should_use_configured_limiter()
    {
        await using var limiter = new CountingRateLimiter(permitCount: 1);
        await using var broker = new InMemoryBroker(new InMemoryBrokerOptions
        {
            Send =
            {
                Mode = InMemorySimulationMode.Reject,
                RateLimiter = limiter
            }
        });

        var dispatcher = await CreateDispatcher(broker);
        await Dispatch(dispatcher, "msg-1", "queue");

        _ = Assert.ThrowsAsync<InMemorySimulationException>(async () => await Dispatch(dispatcher, "msg-2", "queue"));
        Assert.That(limiter.AttemptAcquireCalls, Is.EqualTo(2));
    }
}

[TestFixture]
public class When_simulating_send_with_rate_limiter_factory
{
    [Test]
    public async Task Should_create_and_reuse_limiter_per_operation_and_queue()
    {
        var factoryCalls = 0;
        var createdLimiters = new List<CountingRateLimiter>();
        await using var broker = new InMemoryBroker(new InMemoryBrokerOptions
        {
            Send =
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

        var dispatcher = await CreateDispatcher(broker);
        await Dispatch(dispatcher, "msg-1", "queue");
        Assert.ThrowsAsync<InMemorySimulationException>(async () => await Dispatch(dispatcher, "msg-2", "queue"));

        await Dispatch(dispatcher, "msg-3", "other-queue");
        Assert.ThrowsAsync<InMemorySimulationException>(async () => await Dispatch(dispatcher, "msg-4", "other-queue"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(factoryCalls, Is.EqualTo(2));
            Assert.That(createdLimiters.Count, Is.EqualTo(2));
            Assert.That(createdLimiters[0].AttemptAcquireCalls, Is.EqualTo(2));
            Assert.That(createdLimiters[1].AttemptAcquireCalls, Is.EqualTo(2));
        }

        foreach (var limiter in createdLimiters)
        {
            await limiter.DisposeAsync();
        }
    }
}

[TestFixture]
public class When_simulation_configuration_has_rate_limit_and_direct_limiter
{
    [Test]
    public void Should_fail_when_rate_limit_and_direct_limiter_are_both_configured()
    {
        using var limiter = new CountingRateLimiter(permitCount: 1);

        var exception = Assert.Throws<ArgumentException>(() => new InMemoryBroker(new InMemoryBrokerOptions
        {
            Send =
            {
                RateLimit = new InMemoryRateLimitOptions { PermitLimit = 1, Window = TimeSpan.FromSeconds(5) },
                RateLimiter = limiter
            }
        }));

        Assert.That(exception!.Message, Does.Contain("RateLimiter"));
    }
}

[TestFixture]
public class When_simulation_configuration_has_direct_limiter_and_factory
{
    [Test]
    public void Should_fail_when_direct_limiter_and_factory_are_both_configured()
    {
        using var limiter = new CountingRateLimiter(permitCount: 1);

        var exception = Assert.Throws<ArgumentException>(() => new InMemoryBroker(new InMemoryBrokerOptions
        {
            Send =
            {
                RateLimiter = limiter,
                RateLimiterFactory = _ => new CountingRateLimiter(permitCount: 1)
            }
        }));

        Assert.That(exception!.Message, Does.Contain("RateLimiterFactory"));
    }
}

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

        broker.EnqueueDelayed(CreateEnvelope("msg-1", "queue", 1), simulatedTime.GetUtcNow());
        broker.EnqueueDelayed(CreateEnvelope("msg-2", "queue", 2), simulatedTime.GetUtcNow());
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

[TestFixture]
public class When_built_in_fixed_window_limiter_rejects
{
    [Test]
    public async Task Should_surface_retry_after_metadata_in_the_simulation_exception()
    {
        await using var limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
        {
            PermitLimit = 1,
            Window = TimeSpan.FromSeconds(30),
            QueueLimit = 0,
            AutoReplenishment = true
        });

        await using var broker = new InMemoryBroker(new InMemoryBrokerOptions
        {
            Send =
            {
                Mode = InMemorySimulationMode.Reject,
                RateLimiter = limiter
            }
        });

        var dispatcher = await CreateDispatcher(broker);
        await Dispatch(dispatcher, "msg-1", "queue");

        var exception = Assert.ThrowsAsync<InMemorySimulationException>(async () => await Dispatch(dispatcher, "msg-2", "queue"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.RetryAfter, Is.GreaterThan(TimeSpan.Zero));
            Assert.That(exception.RetryAfter, Is.LessThanOrEqualTo(TimeSpan.FromSeconds(30)));
        }
    }
}

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
        Assert.That(limiter.AttemptAcquireCalls, Is.GreaterThanOrEqualTo(1));

        await receiver.StopReceive();
    }
}

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
        Assert.That(factoryCalls, Is.EqualTo(1));

        await receiver.StopReceive();

        foreach (var l in createdLimiters)
        {
            await l.DisposeAsync();
        }
    }
}

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

        broker.EnqueueDelayed(CreateEnvelope("msg-1", "queue", 1), simulatedTime.GetUtcNow());
        await broker.StartPump();

        var queue = broker.GetOrCreateQueue("queue");
        await AsyncSpinWait.Until(() => queue.Count == 1, maxIterations: 100);

        Assert.That(queue.Count, Is.EqualTo(1));
        Assert.That(limiter.AttemptAcquireCalls, Is.GreaterThanOrEqualTo(1));
    }
}

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

        broker.EnqueueDelayed(CreateEnvelope("msg-1", "queue", 1), simulatedTime.GetUtcNow());
        broker.EnqueueDelayed(CreateEnvelope("msg-2", "other-queue", 2), simulatedTime.GetUtcNow());
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
