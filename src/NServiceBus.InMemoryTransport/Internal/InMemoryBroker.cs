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
        delayedMessages.Enqueue(envelope with { DeliverAt = deliverAt }, deliverAt);
    }

    public bool TryDequeueDelayed(DateTimeOffset now, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out BrokerEnvelope? envelope)
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

    public async Task StartDelayedMessagePump(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            var now = DateTimeOffset.UtcNow;
            while (TryDequeueDelayed(now, out var envelope))
            {
                var queue = GetOrCreateQueue(envelope!.Destination);
                await queue.Enqueue(envelope, CancellationToken.None).ConfigureAwait(false);
            }
        }
    }

    public BrokerPayloadStore PayloadStore { get; } = new();

    public async ValueTask DisposeAsync()
    {
        await delayedPumpCancelSource.CancelAsync().ConfigureAwait(false);
        if (delayedPumpTask != null)
        {
            await delayedPumpTask.ConfigureAwait(false);
        }
        delayedPumpCancelSource.Dispose();
    }

    public void StartPump()
    {
        if (Interlocked.CompareExchange(ref pumpStarted, 1, 0) == 0)
        {
            delayedPumpCancelSource = new CancellationTokenSource();
            delayedPumpTask = Task.Run(() => StartDelayedMessagePump(delayedPumpCancelSource.Token));
        }
    }

    readonly ConcurrentDictionary<string, InMemoryChannel> queues = new();
    readonly ConcurrentDictionary<string, List<string>> subscriptions = new();
    readonly PriorityQueue<BrokerEnvelope, DateTimeOffset> delayedMessages = new();
    long sequenceNumber;
    int pumpStarted;
    CancellationTokenSource delayedPumpCancelSource = new();
    Task? delayedPumpTask;
}
