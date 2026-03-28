namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Routing;
using Transport;

[TestFixture]
public class InMemoryBrokerTests
{
    [Test]
    public async Task Queue_EnqueueAndDequeue_ReturnsEnvelopes()
    {
        var broker = new InMemoryBroker();
        var queue = broker.GetOrCreateQueue("test-queue");

        var envelope = BrokerPayloadStore.Borrow(
            "msg-1",
            new byte[] { 1, 2, 3 },
            new Dictionary<string, string>(),
            "test-queue",
            isPublished: false,
            sequenceNumber: 1);

        await queue.Enqueue(envelope, CancellationToken.None).ConfigureAwait(false);

        var dequeued = await queue.Dequeue(CancellationToken.None).ConfigureAwait(false);

        Assert.That(dequeued.MessageId, Is.EqualTo("msg-1"));
        Assert.That(dequeued.Body.ToArray(), Is.EqualTo(new byte[] { 1, 2, 3 }));

        envelope.Dispose();
        await broker.DisposeAsync();
    }

    [Test]
    public async Task Queue_Dequeue_RespectsCancellation()
    {
        var broker = new InMemoryBroker();
        var queue = broker.GetOrCreateQueue("test-queue");

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await queue.Dequeue(cts.Token));

        await broker.DisposeAsync();
    }

    [Test]
    public async Task Queue_Count_ReflectsEnqueuedCount()
    {
        var broker = new InMemoryBroker();
        var queue = broker.GetOrCreateQueue("test-queue");

        Assert.That(queue.Count, Is.EqualTo(0));

        var envelope1 = BrokerPayloadStore.Borrow(
            "msg-1", new byte[] { 1 }, new Dictionary<string, string>(), "test-queue", false, 1);
        var envelope2 = BrokerPayloadStore.Borrow(
            "msg-2", new byte[] { 2 }, new Dictionary<string, string>(), "test-queue", false, 2);

        await queue.Enqueue(envelope1, CancellationToken.None).ConfigureAwait(false);
        Assert.That(queue.Count, Is.EqualTo(1));

        await queue.Enqueue(envelope2, CancellationToken.None).ConfigureAwait(false);
        Assert.That(queue.Count, Is.EqualTo(2));

        await queue.Dequeue(CancellationToken.None).ConfigureAwait(false);
        Assert.That(queue.Count, Is.EqualTo(1));

        envelope1.Dispose();
        envelope2.Dispose();
        await broker.DisposeAsync();
    }

    [Test]
    public void Broker_GetOrCreateQueue_ReturnsSameQueue()
    {
        var broker = new InMemoryBroker();

        var queue1 = broker.GetOrCreateQueue("test-queue");
        var queue2 = broker.GetOrCreateQueue("test-queue");

        Assert.That(queue1, Is.SameAs(queue2));
        Assert.That(queue2, Is.SameAs(queue1));
    }

    [Test]
    public void Broker_TryGetQueue_ReturnsTrueWhenExists()
    {
        var broker = new InMemoryBroker();
        broker.GetOrCreateQueue("test-queue");

        Assert.That(broker.TryGetQueue("test-queue", out var queue), Is.True);
        Assert.That(queue, Is.Not.Null);
    }

    [Test]
    public void Broker_TryGetQueue_ReturnsFalseWhenNotExists()
    {
        var broker = new InMemoryBroker();

        Assert.That(broker.TryGetQueue("nonexistent", out var queue), Is.False);
        Assert.That(queue, Is.Null);
    }

    [Test]
    public void Broker_Subscribe_AddsSubscriber()
    {
        var broker = new InMemoryBroker();

        broker.Subscribe("subscriber-1", "topic-1");

        var subscribers = broker.GetSubscribers("topic-1");

        Assert.That(subscribers.Count, Is.EqualTo(1));
        Assert.That(subscribers[0], Is.EqualTo("subscriber-1"));
    }

    [Test]
    public void Broker_Subscribe_MultipleSubscribers_SameTopic()
    {
        var broker = new InMemoryBroker();

        broker.Subscribe("subscriber-1", "topic-1");
        broker.Subscribe("subscriber-2", "topic-1");

        var subscribers = broker.GetSubscribers("topic-1");

        Assert.That(subscribers.Count, Is.EqualTo(2));
        Assert.That(subscribers, Contains.Item("subscriber-1"));
        Assert.That(subscribers, Contains.Item("subscriber-2"));
    }

    [Test]
    public void Broker_Subscribe_SameSubscriberTwice_Deduplicates()
    {
        var broker = new InMemoryBroker();

        broker.Subscribe("subscriber-1", "topic-1");
        broker.Subscribe("subscriber-1", "topic-1");

        var subscribers = broker.GetSubscribers("topic-1");

        Assert.That(subscribers.Count, Is.EqualTo(1));
        Assert.That(subscribers[0], Is.EqualTo("subscriber-1"));
    }

    [Test]
    public void Broker_Unsubscribe_RemovesSubscriber()
    {
        var broker = new InMemoryBroker();

        broker.Subscribe("subscriber-1", "topic-1");
        broker.Subscribe("subscriber-2", "topic-1");

        broker.Unsubscribe("subscriber-1", "topic-1");

        var subscribers = broker.GetSubscribers("topic-1");

        Assert.That(subscribers.Count, Is.EqualTo(1));
        Assert.That(subscribers[0], Is.EqualTo("subscriber-2"));
    }

    [Test]
    public void Broker_GetSubscribers_NoSubscribers_ReturnsEmpty()
    {
        var broker = new InMemoryBroker();

        var subscribers = broker.GetSubscribers("nonexistent-topic");

        Assert.That(subscribers, Is.Empty);
    }

    [Test]
    public void Broker_GetNextSequenceNumber_ReturnsSequentialNumbers()
    {
        var broker = new InMemoryBroker();

        var seq1 = broker.GetNextSequenceNumber();
        var seq2 = broker.GetNextSequenceNumber();
        var seq3 = broker.GetNextSequenceNumber();

        Assert.That(seq2, Is.EqualTo(seq1 + 1));
        Assert.That(seq3, Is.EqualTo(seq2 + 1));
    }

    [Test]
    public async Task Broker_DelayedDelivery_EnqueueAndDequeue()
    {
        var broker = new InMemoryBroker();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var envelope = BrokerPayloadStore.Borrow(
            "msg-1",
            new byte[] { 1, 2, 3 },
            new Dictionary<string, string>(),
            "test-queue",
            isPublished: false,
            sequenceNumber: 1);

        var deliverAt = DateTimeOffset.UtcNow.AddMilliseconds(200);
        broker.EnqueueDelayed(envelope, deliverAt);

        await broker.StartPump(cts.Token).ConfigureAwait(false);

        await Task.Delay(300).ConfigureAwait(false);

        var queue = broker.GetOrCreateQueue("test-queue");
        Assert.That(queue.Count, Is.EqualTo(1));

        var dequeued = await queue.Dequeue(CancellationToken.None).ConfigureAwait(false);
        Assert.That(dequeued.MessageId, Is.EqualTo("msg-1"));

        envelope.Dispose();
        await broker.DisposeAsync();
    }

    [Test]
    public async Task Broker_delayed_pump_should_wake_when_earlier_message_is_enqueued()
    {
        var broker = new InMemoryBroker();

        var laterEnvelope = BrokerPayloadStore.Borrow(
            "later",
            new byte[] { 1 },
            new Dictionary<string, string>(),
            "q",
            isPublished: false,
            sequenceNumber: 1);
        var earlierEnvelope = BrokerPayloadStore.Borrow(
            "earlier",
            new byte[] { 2 },
            new Dictionary<string, string>(),
            "q",
            isPublished: false,
            sequenceNumber: 2);

        broker.EnqueueDelayed(laterEnvelope, DateTimeOffset.UtcNow.AddSeconds(2));
        await broker.StartPump(CancellationToken.None).ConfigureAwait(false);

        await AllowBackgroundPumpToStart(CancellationToken.None).ConfigureAwait(false);

        broker.EnqueueDelayed(earlierEnvelope, DateTimeOffset.UtcNow);

        var queue = broker.GetOrCreateQueue("q");
        using var dequeueCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        var dequeued = await queue.Dequeue(dequeueCts.Token).ConfigureAwait(false);

        Assert.That(dequeued.MessageId, Is.EqualTo("earlier"));

        dequeued.Dispose();
        await broker.DisposeAsync();
    }

    [Test]
    public async Task Broker_start_pump_should_be_safe_to_call_multiple_times()
    {
        var broker = new InMemoryBroker();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        await broker.StartPump(cts.Token).ConfigureAwait(false);
        await broker.StartPump(cts.Token).ConfigureAwait(false);

        await broker.DisposeAsync().ConfigureAwait(false);

        Assert.Pass();
    }

    [Test]
    public void Broker_DelayedDelivery_TryDequeueDelayed_NotYetDue()
    {
        var broker = new InMemoryBroker();

        var envelope = BrokerPayloadStore.Borrow(
            "msg-1",
            new byte[] { 1 },
            new Dictionary<string, string>(),
            "test-queue",
            isPublished: false,
            sequenceNumber: 1);

        var futureTime = DateTimeOffset.UtcNow.AddHours(1);
        broker.EnqueueDelayed(envelope, futureTime);

        var result = broker.TryDequeueDelayed(DateTimeOffset.UtcNow, out var dequeued);

        Assert.That(result, Is.False);
        Assert.That(dequeued, Is.Null);

        envelope.Dispose();
    }

    [Test]
    public void Broker_DelayedDelivery_TryDequeueDelayed_DueMessage()
    {
        var broker = new InMemoryBroker();

        var envelope = BrokerPayloadStore.Borrow(
            "msg-1",
            new byte[] { 1 },
            new Dictionary<string, string>(),
            "test-queue",
            isPublished: false,
            sequenceNumber: 1);

        var pastTime = DateTimeOffset.UtcNow.AddMinutes(-1);
        broker.EnqueueDelayed(envelope, pastTime);

        var result = broker.TryDequeueDelayed(DateTimeOffset.UtcNow, out var dequeued);

        Assert.That(result, Is.True);
        Assert.That(dequeued, Is.Not.Null);
        Assert.That(dequeued!.MessageId, Is.EqualTo("msg-1"));
    }

    [Test]
    public void Broker_DelayedDelivery_TryDequeueDelayed_RespectsOrdering()
    {
        var broker = new InMemoryBroker();

        var envelope1 = BrokerPayloadStore.Borrow(
            "msg-1", new byte[] { 1 }, new Dictionary<string, string>(), "q", false, 1);
        var envelope2 = BrokerPayloadStore.Borrow(
            "msg-2", new byte[] { 2 }, new Dictionary<string, string>(), "q", false, 2);

        var time1 = DateTimeOffset.UtcNow.AddMinutes(-2);
        var time2 = DateTimeOffset.UtcNow.AddMinutes(-1);

        broker.EnqueueDelayed(envelope1, time1);
        broker.EnqueueDelayed(envelope2, time2);

        var result1 = broker.TryDequeueDelayed(DateTimeOffset.UtcNow, out var dequeued1);
        var result2 = broker.TryDequeueDelayed(DateTimeOffset.UtcNow, out var dequeued2);

        Assert.That(result1, Is.True);
        Assert.That(dequeued1!.MessageId, Is.EqualTo("msg-1"));

        Assert.That(result2, Is.True);
        Assert.That(dequeued2!.MessageId, Is.EqualTo("msg-2"));
    }

    [Test]
    public void Broker_delayed_messages_with_same_due_time_should_remain_deterministic()
    {
        var broker = new InMemoryBroker();
        var dueTime = DateTimeOffset.UtcNow.AddMinutes(-1);

        var envelope1 = BrokerPayloadStore.Borrow(
            "msg-1", new byte[] { 1 }, new Dictionary<string, string>(), "q", false, 1);
        var envelope2 = BrokerPayloadStore.Borrow(
            "msg-2", new byte[] { 2 }, new Dictionary<string, string>(), "q", false, 2);
        var envelope3 = BrokerPayloadStore.Borrow(
            "msg-3", new byte[] { 3 }, new Dictionary<string, string>(), "q", false, 3);

        broker.EnqueueDelayed(envelope1, dueTime);
        broker.EnqueueDelayed(envelope2, dueTime);
        broker.EnqueueDelayed(envelope3, dueTime);

        Assert.That(broker.TryDequeueDelayed(DateTimeOffset.UtcNow, out var dequeued1), Is.True);
        Assert.That(broker.TryDequeueDelayed(DateTimeOffset.UtcNow, out var dequeued2), Is.True);
        Assert.That(broker.TryDequeueDelayed(DateTimeOffset.UtcNow, out var dequeued3), Is.True);

        Assert.Multiple(() =>
        {
            Assert.That(dequeued1!.MessageId, Is.EqualTo("msg-1"));
            Assert.That(dequeued2!.MessageId, Is.EqualTo("msg-2"));
            Assert.That(dequeued3!.MessageId, Is.EqualTo("msg-3"));
        });
    }

    [Test]
    public async Task Broker_stop_while_delayed_message_becomes_due_should_not_lose_or_duplicate_message()
    {
        var broker = new InMemoryBroker();
        var envelope = BrokerPayloadStore.Borrow(
            "msg-1",
            new byte[] { 1 },
            new Dictionary<string, string>(),
            "q",
            isPublished: false,
            sequenceNumber: 1);

        broker.EnqueueDelayed(envelope, DateTimeOffset.UtcNow.AddMilliseconds(20));

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(60));

        await broker.StartPump(cts.Token).ConfigureAwait(false);

        var cancellationObserved = await WaitForCancellation(cts.Token).WaitAsync(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
        Assert.That(cancellationObserved, Is.True);

        Assert.That(broker.TryGetQueue("q", out var queue), Is.True);
        Assert.That(queue!.Count, Is.EqualTo(1));

        var dequeued = await queue.Dequeue(CancellationToken.None).ConfigureAwait(false);
        Assert.That(dequeued.MessageId, Is.EqualTo("msg-1"));

        dequeued.Dispose();
        await broker.DisposeAsync().ConfigureAwait(false);
    }

    [Test]
    public void BrokerEnvelope_WithDeliveryAttempt_Should_isolate_headers()
    {
        var envelope = BrokerPayloadStore.Borrow(
            "msg-1",
            new byte[] { 1 },
            new Dictionary<string, string> { ["key"] = "original" },
            "test-queue",
            isPublished: false,
            sequenceNumber: 1);

        var retryEnvelope = envelope.WithDeliveryAttempt(2);

        ((Dictionary<string, string>)retryEnvelope.Headers)["key"] = "retry";

        Assert.That(envelope.Headers["key"], Is.EqualTo("original"));

        envelope.Dispose();
    }

    [Test]
    public void Broker_EnqueueDelayed_Should_snapshot_headers()
    {
        var broker = new InMemoryBroker();
        var envelope = BrokerPayloadStore.Borrow(
            "msg-1",
            new byte[] { 1 },
            new Dictionary<string, string> { ["key"] = "original" },
            "test-queue",
            isPublished: false,
            sequenceNumber: 1);

        broker.EnqueueDelayed(envelope, DateTimeOffset.UtcNow.AddMinutes(1));

        ((Dictionary<string, string>)envelope.Headers)["key"] = "changed";

        _ = broker.TryDequeueDelayed(DateTimeOffset.UtcNow.AddHours(1), out var delayedEnvelope);

        Assert.That(delayedEnvelope!.Headers["key"], Is.EqualTo("original"));

        delayedEnvelope.Dispose();
    }

    [Test]
    public async Task Transport_initialize_should_use_broker_from_service_provider_when_available()
    {
        var serviceProviderBroker = new InMemoryBroker();
        var constructorBroker = new InMemoryBroker();
        var hostSettings = new HostSettings("endpoint", string.Empty, new StartupDiagnosticEntries(), static (_, _, _) => { }, true)
        {
            ServiceProvider = new ServiceCollection()
                .AddSingleton(serviceProviderBroker)
                .BuildServiceProvider()
        };

        var transport = new InMemoryTransport(constructorBroker);
        var infrastructure = await transport.Initialize(
            hostSettings,
            [new ReceiveSettings("main", new QueueAddress("input"), false, true, "error")],
            ["error"],
            CancellationToken.None).ConfigureAwait(false);

        var message = new OutgoingMessage("id", [], new byte[] { 1 });
        var operation = new TransportOperation(message, new UnicastAddressTag("destination"));

        await infrastructure.Dispatcher.Dispatch(new TransportOperations(operation), new TransportTransaction(), CancellationToken.None).ConfigureAwait(false);

        Assert.Multiple(() =>
        {
            Assert.That(serviceProviderBroker.TryGetQueue("destination", out var queue), Is.True);
            Assert.That(queue!.Count, Is.EqualTo(1));
            Assert.That(constructorBroker.TryGetQueue("destination", out _), Is.False);
        });

        await serviceProviderBroker.DisposeAsync();
        await constructorBroker.DisposeAsync();
    }

    [Test]
    public async Task Transport_initialize_should_fall_back_when_service_provider_has_no_broker()
    {
        var constructorBroker = new InMemoryBroker();
        var hostSettings = new HostSettings("endpoint", string.Empty, new StartupDiagnosticEntries(), static (_, _, _) => { }, true)
        {
            ServiceProvider = new ServiceCollection().BuildServiceProvider()
        };

        var transport = new InMemoryTransport(constructorBroker);
        var infrastructure = await transport.Initialize(
            hostSettings,
            [new ReceiveSettings("main", new QueueAddress("input"), false, true, "error")],
            ["error"],
            CancellationToken.None).ConfigureAwait(false);

        var message = new OutgoingMessage("id", [], new byte[] { 1 });
        var operation = new TransportOperation(message, new UnicastAddressTag("destination"));

        await infrastructure.Dispatcher.Dispatch(new TransportOperations(operation), new TransportTransaction(), CancellationToken.None).ConfigureAwait(false);

        Assert.That(constructorBroker.TryGetQueue("destination", out var queue), Is.True);
        Assert.That(queue!.Count, Is.EqualTo(1));

        await constructorBroker.DisposeAsync();
    }

    static async Task AllowBackgroundPumpToStart(CancellationToken cancellationToken)
    {
        for (var i = 0; i < 100; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();
        }
    }

    static Task<bool> WaitForCancellation(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromResult(true);
        }

        var completionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        cancellationToken.Register(static state => ((TaskCompletionSource<bool>)state!).TrySetResult(true), completionSource);
        return completionSource.Task;
    }
}
