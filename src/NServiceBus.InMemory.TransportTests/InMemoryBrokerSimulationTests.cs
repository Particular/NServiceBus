namespace NServiceBus.TransportTests;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus.DelayedDelivery;
using Microsoft.Extensions.Time.Testing;
using NServiceBus.Transport;
using NUnit.Framework;
using Routing;
using System.Threading.RateLimiting;
using static InMemoryBrokerSimulationTestHelper;

static class InMemoryBrokerSimulationTestHelper
{
    public static async Task<IMessageDispatcher> CreateDispatcher(InMemoryBroker broker, CancellationToken cancellationToken = default)
    {
        var infrastructure = await CreateInfrastructure(broker, cancellationToken);
        return infrastructure.Dispatcher;
    }

    public static async Task<IMessageReceiver> CreateReceiver(InMemoryBroker broker, CancellationToken cancellationToken = default)
    {
        var infrastructure = await CreateInfrastructure(broker, cancellationToken);
        return infrastructure.Receivers["main"];
    }

    public static Task<TransportInfrastructure> CreateInfrastructure(InMemoryBroker broker, CancellationToken cancellationToken = default)
    {
        var transport = new InMemoryTransport(broker);
        return transport.Initialize(
            new HostSettings("endpoint", string.Empty, new StartupDiagnosticEntries(), static (_, _, _) => { }, true),
            [new ReceiveSettings("main", new QueueAddress("input"), false, true, "error")],
            ["error"],
            cancellationToken);
    }

    public static Task Dispatch(IMessageDispatcher dispatcher, string messageId, string destination, CancellationToken cancellationToken = default)
    {
        var message = new OutgoingMessage(messageId, [], new byte[] { 1 });
        var operation = new TransportOperation(message, new UnicastAddressTag(destination));
        return dispatcher.Dispatch(new TransportOperations(operation), new TransportTransaction(), cancellationToken);
    }

    public static BrokerEnvelope CreateEnvelope(string messageId, string destination, long sequenceNumber)
    {
        return BrokerPayloadStore.Borrow(messageId, new byte[] { 1 }, new Dictionary<string, string>(), destination, false, sequenceNumber);
    }
}

[TestFixture]
public class When_dispatching_with_delayed_delivery_using_simulated_time
{
    [Test]
    public async Task Should_schedule_relative_delivery_from_broker_time()
    {
        var simulatedTime = new FakeTimeProvider(new DateTimeOffset(2026, 03, 28, 12, 0, 0, TimeSpan.Zero));
        await using var broker = new InMemoryBroker(new InMemoryBrokerOptions { TimeProvider = simulatedTime });

        var dispatcher = await CreateDispatcher(broker);
        var message = new OutgoingMessage("msg-1", [], new byte[] { 1 });
        var properties = new DispatchProperties
        {
            DelayDeliveryWith = new DelayDeliveryWith(TimeSpan.FromSeconds(5))
        };

        await dispatcher.Dispatch(
            new TransportOperations(new TransportOperation(message, new UnicastAddressTag("queue"), properties)),
            new TransportTransaction(),
            CancellationToken.None);

        var dequeued = broker.TryDequeueDelayed(simulatedTime.GetUtcNow().AddSeconds(4), out var tooEarlyEnvelope);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(dequeued, Is.False);
            Assert.That(tooEarlyEnvelope, Is.Null);
        }

        dequeued = broker.TryDequeueDelayed(simulatedTime.GetUtcNow().AddSeconds(5), out var dueEnvelope);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(dequeued, Is.True);
            Assert.That(dueEnvelope, Is.Not.Null);
        }
    }
}

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
public class When_simulating_receive_delay_with_queue_override
{
    [Test]
    public async Task Should_use_queue_operation_settings_over_broker_defaults()
    {
        var simulatedTime = new FakeTimeProvider(new DateTimeOffset(2026, 03, 28, 12, 0, 0, TimeSpan.Zero));
        var options = new InMemoryBrokerOptions
        {
            TimeProvider = simulatedTime,
            Receive = { RateLimit = new InMemoryRateLimitOptions { PermitLimit = 1, Window = TimeSpan.FromSeconds(30) } }
        };
        options.ForQueue("input").Receive.RateLimit = new InMemoryRateLimitOptions { PermitLimit = 2, Window = TimeSpan.FromSeconds(30) };

        await using var broker = new InMemoryBroker(options);
        var receiver = await CreateReceiver(broker);
        var secondReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thirdReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var receivedCount = 0;

        await receiver.Initialize(
            new PushRuntimeSettings(maxConcurrency: 1),
            (_, _) =>
            {
                var currentCount = Interlocked.Increment(ref receivedCount);
                if (currentCount == 2)
                {
                    secondReceived.TrySetResult();
                }
                else if (currentCount == 3)
                {
                    thirdReceived.TrySetResult();
                }

                return Task.CompletedTask;
            },
            (_, _) => Task.FromResult(ErrorHandleResult.Handled));

        var queue = broker.GetOrCreateQueue("input");
        await queue.Enqueue(CreateEnvelope("msg-1", "input", 1), CancellationToken.None);
        await queue.Enqueue(CreateEnvelope("msg-2", "input", 2), CancellationToken.None);
        await queue.Enqueue(CreateEnvelope("msg-3", "input", 3), CancellationToken.None);
        await receiver.StartReceive();

        await secondReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(Volatile.Read(ref receivedCount), Is.EqualTo(2));
            Assert.That(thirdReceived.Task.IsCompleted, Is.False);
        }

        simulatedTime.Advance(TimeSpan.FromSeconds(30));
        await thirdReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.That(Volatile.Read(ref receivedCount), Is.EqualTo(3));
        await receiver.StopReceive();
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

[TestFixture]
public class When_simulating_receive_delay
{
    [Test]
    public async Task Should_wait_for_simulated_time_to_advance()
    {
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2026, 03, 28, 12, 0, 0, TimeSpan.Zero));
        await using var broker = new InMemoryBroker(new InMemoryBrokerOptions
        {
            TimeProvider = fakeTime,
            Receive = { RateLimit = new InMemoryRateLimitOptions { PermitLimit = 1, Window = TimeSpan.FromSeconds(5) } }
        });

        var receiver = await CreateReceiver(broker, CancellationToken.None);
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
            (_, _) => Task.FromResult(ErrorHandleResult.Handled),
            CancellationToken.None);

        var queue = broker.GetOrCreateQueue("input");
        await queue.Enqueue(CreateEnvelope("msg-1", "input", 1), CancellationToken.None);
        await queue.Enqueue(CreateEnvelope("msg-2", "input", 2), CancellationToken.None);
        await receiver.StartReceive();

        await firstReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(Volatile.Read(ref receivedCount), Is.EqualTo(1));
            Assert.That(secondReceived.Task.IsCompleted, Is.False);
        }

        fakeTime.Advance(TimeSpan.FromSeconds(5));
        await secondReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.That(Volatile.Read(ref receivedCount), Is.EqualTo(2));
        await receiver.StopReceive();
    }
}

[TestFixture]
public class When_receiving_without_simulation
{
    [Test]
    public async Task Should_process_message()
    {
        await using var broker = new InMemoryBroker();
        var receiver = await CreateReceiver(broker, CancellationToken.None);
        var received = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var receivedCount = 0;

        await receiver.Initialize(
            new PushRuntimeSettings(maxConcurrency: 1),
            (_, _) =>
            {
                Interlocked.Increment(ref receivedCount);
                received.TrySetResult();
                return Task.CompletedTask;
            },
            (_, _) => Task.FromResult(ErrorHandleResult.Handled),
            CancellationToken.None);

        await broker.GetOrCreateQueue("input").Enqueue(CreateEnvelope("msg-1", "input", 1), CancellationToken.None);
        await receiver.StartReceive();

        await received.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.That(Volatile.Read(ref receivedCount), Is.EqualTo(1));

        await receiver.StopReceive();
    }
}

sealed class CountingRateLimiter(int permitCount) : RateLimiter
{
    int remainingPermits = permitCount;

    public int AttemptAcquireCalls { get; private set; }

    protected override RateLimitLease AttemptAcquireCore(int permitCount)
    {
        AttemptAcquireCalls++;
        if (remainingPermits > 0)
        {
            remainingPermits--;
            return SuccessfulLease.Instance;
        }

        return FailedLease.Instance;
    }

    protected override ValueTask<RateLimitLease> AcquireAsyncCore(int permitCount, CancellationToken cancellationToken = default)
    {
        AttemptAcquireCalls++;
        if (remainingPermits > 0)
        {
            remainingPermits--;
            return ValueTask.FromResult<RateLimitLease>(SuccessfulLease.Instance);
        }

        return ValueTask.FromResult<RateLimitLease>(FailedLease.Instance);
    }

    public override TimeSpan? IdleDuration => null;

    public override RateLimiterStatistics GetStatistics() => null;

    protected override void Dispose(bool disposing)
    {
    }

    sealed class SuccessfulLease : RateLimitLease
    {
        public static SuccessfulLease Instance { get; } = new();

        public override bool IsAcquired => true;

        public override IEnumerable<string> MetadataNames => [];

        public override bool TryGetMetadata(string metadataName, out object metadata)
        {
            metadata = null;
            return false;
        }
    }

    sealed class FailedLease : RateLimitLease
    {
        public static FailedLease Instance { get; } = new();

        public override bool IsAcquired => false;

        public override IEnumerable<string> MetadataNames => [];

        public override bool TryGetMetadata(string metadataName, out object metadata)
        {
            metadata = null;
            return false;
        }
    }
}
