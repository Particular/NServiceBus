namespace NServiceBus.TransportTests;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Time.Testing;
using NServiceBus.Transport;
using NUnit.Framework;
using Routing;
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
        return BrokerPayloadStore.Borrow(messageId, [1], new Dictionary<string, string>(), destination, false, sequenceNumber);
    }
}

[TestFixture]
public class When_delayed_pump_uses_broker_time_provider
{
    [Test]
    public async Task Should_release_when_fake_time_advances()
    {
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2026, 03, 28, 12, 0, 0, TimeSpan.Zero));
        await using var broker = new InMemoryBroker(new InMemoryBrokerOptions { TimeProvider = fakeTime });

        var envelope = CreateEnvelope("msg-1", "queue", 1);
        broker.EnqueueDelayed(envelope, fakeTime.GetUtcNow().AddSeconds(5));
        await broker.StartPump();

        var queue = broker.GetOrCreateQueue("queue");
        Assert.That(queue.Count, Is.Zero);

        fakeTime.Advance(TimeSpan.FromSeconds(5));
        await AsyncSpinWait.Until(() => queue.Count == 1, maxIterations: 10);

        Assert.That(queue.Count, Is.EqualTo(1));
    }
}

[TestFixture]
public class When_simulating_send_delay
{
    [Test]
    public async Task Should_wait_for_fake_time_to_advance()
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
public class When_simulating_delayed_delivery_delay
{
    [Test]
    public async Task Should_wait_for_fake_time_to_advance()
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
public class When_simulating_receive_delay
{
    [Test]
    public async Task Should_wait_for_fake_time_to_advance()
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

        await firstReceived.Task;
        Assert.That(Volatile.Read(ref receivedCount), Is.EqualTo(1));
        Assert.That(secondReceived.Task.IsCompleted, Is.False);

        fakeTime.Advance(TimeSpan.FromSeconds(5));
        await secondReceived.Task;

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

        await received.Task;
        Assert.That(Volatile.Read(ref receivedCount), Is.EqualTo(1));

        await receiver.StopReceive();
    }
}