namespace NServiceBus;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public sealed class InMemoryBroker : IAsyncDisposable
{
    public InMemoryChannel GetOrCreateQueue(string address)
    {
        return queues.GetOrAdd(address, _ => new InMemoryChannel());
    }

    public bool TryGetQueue(string address, out InMemoryChannel? queue)
    {
        return queues.TryGetValue(address, out queue);
    }

    public void Subscribe(string publisherAddress, string topic)
    {
        subscriptions.AddOrUpdate(
            topic,
            _ => [publisherAddress],
            (_, list) =>
            {
                lock (list)
                {
                    if (!list.Contains(publisherAddress))
                    {
                        list.Add(publisherAddress);
                    }
                }
                return list;
            });
    }

    public void Unsubscribe(string publisherAddress, string topic)
    {
        if (subscriptions.TryGetValue(topic, out var list))
        {
            lock (list)
            {
                list.Remove(publisherAddress);
            }
        }
    }

    public IReadOnlyList<string> GetSubscribers(string topic)
    {
        if (subscriptions.TryGetValue(topic, out var list))
        {
            lock (list)
            {
                return list.ToArray();
            }
        }
        return [];
    }

    public long GetNextSequenceNumber() => Interlocked.Increment(ref sequenceNumber);

    public void EnqueueDelayed(BrokerEnvelope envelope, DateTimeOffset deliverAt)
    {
        lock (delayedMessagesLock)
        {
            delayedMessages.Enqueue(envelope.WithDeliverAt(deliverAt), (deliverAt, envelope.SequenceNumber));
            SignalDelayedMessagesChanged();
        }
    }

    public bool TryDequeueDelayed(DateTimeOffset now, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out BrokerEnvelope? envelope)
    {
        lock (delayedMessagesLock)
        {
            return TryDequeueDelayedCore(now, out envelope);
        }
    }

    public async Task StartDelayedMessagePump(CancellationToken cancellationToken = default)
    {
        while (true)
        {
            BrokerEnvelope? envelopeToDispatch;
            Task scheduleChangedTask;
            TimeSpan? waitDuration;

            lock (delayedMessagesLock)
            {
                if (TryDequeueDelayedCore(DateTimeOffset.UtcNow, out envelopeToDispatch))
                {
                    scheduleChangedTask = Task.CompletedTask;
                    waitDuration = null;
                }
                else
                {
                    envelopeToDispatch = null;
                    scheduleChangedTask = delayedMessagesChanged.Task;
                    waitDuration = GetNextWaitDuration(DateTimeOffset.UtcNow);
                }
            }

            if (envelopeToDispatch != null)
            {
                var queue = GetOrCreateQueue(envelopeToDispatch.Destination);
                await queue.Enqueue(envelopeToDispatch, CancellationToken.None).ConfigureAwait(false);
                continue;
            }

            try
            {
                await WaitForDelayedMessagesAsync(scheduleChangedTask, waitDuration, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    static async Task WaitForDelayedMessagesAsync(Task scheduleChangedTask, TimeSpan? waitDuration, CancellationToken cancellationToken)
    {
        if (waitDuration is null)
        {
            await scheduleChangedTask.WaitAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        if (waitDuration <= TimeSpan.Zero)
        {
            return;
        }

        var delayTask = Task.Delay(waitDuration.Value, cancellationToken);
        var completedTask = await Task.WhenAny(scheduleChangedTask, delayTask).ConfigureAwait(false);
        await completedTask.ConfigureAwait(false);
    }

    TimeSpan? GetNextWaitDuration(DateTimeOffset now)
    {
        if (delayedMessages.Count == 0)
        {
            return null;
        }

        var nextMessage = delayedMessages.Peek();
        var deliverAt = nextMessage.DeliverAt ?? now;
        return deliverAt - now;
    }

    bool TryDequeueDelayedCore(DateTimeOffset now, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out BrokerEnvelope? envelope)
    {
        if (delayedMessages.Count == 0)
        {
            envelope = null;
            return false;
        }

        var peeked = delayedMessages.Peek();
        if (peeked.DeliverAt <= now)
        {
            _ = delayedMessages.Dequeue();
            envelope = peeked;
            return true;
        }

        envelope = null;
        return false;
    }

    void SignalDelayedMessagesChanged()
    {
        delayedMessagesChanged.TrySetResult();
        delayedMessagesChanged = CreateDelayedMessagesChangedSignal();
    }

    static TaskCompletionSource CreateDelayedMessagesChangedSignal() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public async ValueTask DisposeAsync()
    {
        await delayedPumpCancelSource.CancelAsync().ConfigureAwait(false);
        if (delayedPumpTask != null)
        {
            await delayedPumpTask.ConfigureAwait(false);
        }
        delayedPumpCancelSource.Dispose();
    }

    public Task StartPump(CancellationToken cancellationToken = default)
    {
        if (Interlocked.CompareExchange(ref pumpStarted, 1, 0) == 0)
        {
            delayedPumpCancelSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            delayedPumpTask = StartDelayedMessagePump(delayedPumpCancelSource.Token);
        }
        return Task.CompletedTask;
    }

    readonly ConcurrentDictionary<string, InMemoryChannel> queues = new();
    readonly ConcurrentDictionary<string, List<string>> subscriptions = new();
    readonly PriorityQueue<BrokerEnvelope, (DateTimeOffset DeliverAt, long SequenceNumber)> delayedMessages = new();
    readonly Lock delayedMessagesLock = new();
    long sequenceNumber;
    int pumpStarted;
    CancellationTokenSource delayedPumpCancelSource = new();
    Task? delayedPumpTask;
    TaskCompletionSource delayedMessagesChanged = CreateDelayedMessagesChangedSignal();
}
