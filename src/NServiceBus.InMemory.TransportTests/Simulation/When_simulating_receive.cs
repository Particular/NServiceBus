namespace NServiceBus.TransportTests;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Time.Testing;
using NServiceBus.Transport;
using NUnit.Framework;
using static InMemoryBrokerSimulationTestHelper;

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
