namespace NServiceBus;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public sealed class InMemoryBroker : IAsyncDisposable
{
    public InMemoryBroker(InMemoryBrokerOptions? options = null)
    {
        this.options = options ?? new InMemoryBrokerOptions();
        timeProvider = this.options.TimeProvider ?? TimeProvider.System;
    }

    public InMemoryChannel GetOrCreateQueue(string address) => queues.GetOrAdd(address, _ => new InMemoryChannel());

    public bool TryGetQueue(string address, out InMemoryChannel? queue) => queues.TryGetValue(address, out queue);

    public void Subscribe(string publisherAddress, string topic) =>
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
        if (!subscriptions.TryGetValue(topic, out var list))
        {
            return [];
        }

        lock (list)
        {
            return [.. list];
        }
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

    internal Task SimulateSendAsync(string destination, CancellationToken cancellationToken = default) => ApplySimulationAsync(InMemorySimulationOperation.Send, destination, cancellationToken);

    internal Task SimulateReceiveAsync(string destination, CancellationToken cancellationToken = default) => ApplySimulationAsync(InMemorySimulationOperation.Receive, destination, cancellationToken);

    async Task StartDelayedMessagePump(CancellationToken cancellationToken)
    {
        while (true)
        {
            BrokerEnvelope? envelopeToDispatch;
            Task scheduleChangedTask;
            TimeSpan? waitDuration;

            lock (delayedMessagesLock)
            {
                if (TryDequeueDelayedCore(GetUtcNow(), out envelopeToDispatch))
                {
                    scheduleChangedTask = Task.CompletedTask;
                    waitDuration = null;
                }
                else
                {
                    envelopeToDispatch = null;
                    scheduleChangedTask = delayedMessagesChanged.Task;
                    waitDuration = GetNextWaitDuration(GetUtcNow());
                }
            }

            if (envelopeToDispatch != null)
            {
                try
                {
                    await ApplySimulationAsync(InMemorySimulationOperation.DelayedDelivery, envelopeToDispatch.Destination, cancellationToken).ConfigureAwait(false);
                }
                catch (InMemorySimulationException ex)
                {
                    EnqueueDelayed(envelopeToDispatch, GetUtcNow() + ex.RetryAfter);
                    continue;
                }

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

    async Task WaitForDelayedMessagesAsync(Task scheduleChangedTask, TimeSpan? waitDuration, CancellationToken cancellationToken)
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

        var delayTask = Task.Delay(waitDuration.Value, timeProvider, cancellationToken);
        var completedTask = await Task.WhenAny(scheduleChangedTask, delayTask).ConfigureAwait(false);
        await completedTask.ConfigureAwait(false);
    }

    DateTimeOffset GetUtcNow() => timeProvider.GetUtcNow();

    internal DateTimeOffset GetCurrentTime() => GetUtcNow();

    async Task ApplySimulationAsync(InMemorySimulationOperation operation, string queue, CancellationToken cancellationToken)
    {
        var resolved = ResolveSimulation(operation, queue);
        if (resolved.RateLimit is null)
        {
            return;
        }

        while (true)
        {
            var now = resolved.TimeProvider.GetUtcNow();
            var acquired = TryAcquirePermit(operation, queue, resolved.RateLimit, now, out var retryAfter);
            if (acquired)
            {
                return;
            }

            if (resolved.Mode == InMemorySimulationMode.Reject)
            {
                throw new InMemorySimulationException($"In-memory {operation} simulation rejected access to queue '{queue}'.", retryAfter, resolved.TimeProvider);
            }

            await Task.Delay(retryAfter, resolved.TimeProvider, cancellationToken).ConfigureAwait(false);
        }
    }

    ResolvedSimulationSettings ResolveSimulation(InMemorySimulationOperation operation, string queue)
    {
        options.TryGetQueue(queue, out var queueOptions);

        var brokerLevel = operation switch
        {
            InMemorySimulationOperation.Send => options.Send,
            InMemorySimulationOperation.Receive => options.Receive,
            InMemorySimulationOperation.DelayedDelivery => options.DelayedDelivery,
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
        };

        var queueLevel = queueOptions is null
            ? null
            : operation switch
            {
                InMemorySimulationOperation.Send => queueOptions.Send,
                InMemorySimulationOperation.Receive => queueOptions.Receive,
                InMemorySimulationOperation.DelayedDelivery => queueOptions.DelayedDelivery,
                _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
            };

        var effectiveTimeProvider = queueLevel?.TimeProvider
            ?? queueOptions?.TimeProvider
            ?? brokerLevel.TimeProvider
            ?? options.TimeProvider
            ?? TimeProvider.System;

        var effectiveRateLimit = queueLevel?.RateLimit
            ?? queueOptions?.Default.RateLimit
            ?? brokerLevel.RateLimit
            ?? options.Default.RateLimit;

        var effectiveMode = queueLevel?.Mode
            ?? queueOptions?.Default.Mode
            ?? brokerLevel.Mode
            ?? options.Default.Mode
            ?? (effectiveRateLimit is null ? null : InMemorySimulationMode.Delay);

        return new ResolvedSimulationSettings(effectiveTimeProvider, effectiveMode, effectiveRateLimit);
    }

    bool TryAcquirePermit(InMemorySimulationOperation operation, string queue, InMemoryRateLimitOptions rateLimit, DateTimeOffset now, out TimeSpan retryAfter)
    {
        var state = simulationState.GetOrAdd((operation, queue), static (_, now) => new WindowState(now), now);

        lock (state)
        {
            if (rateLimit.PermitLimit <= 0)
            {
                retryAfter = rateLimit.Window;
                return false;
            }

            if (rateLimit.Window <= TimeSpan.Zero)
            {
                retryAfter = TimeSpan.Zero;
                return true;
            }

            if (now - state.WindowStart >= rateLimit.Window)
            {
                state.WindowStart = now;
                state.PermitsUsed = 0;
            }

            if (state.PermitsUsed < rateLimit.PermitLimit)
            {
                state.PermitsUsed++;
                retryAfter = TimeSpan.Zero;
                return true;
            }

            var nextPermitAt = state.WindowStart + rateLimit.Window;
            retryAfter = nextPermitAt - now;
            return false;
        }
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
        _ = delayedMessagesChanged.TrySetResult();
        delayedMessagesChanged = CreateDelayedMessagesChangedSignal();
    }

    static TaskCompletionSource CreateDelayedMessagesChangedSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

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
        if (Interlocked.CompareExchange(ref pumpStarted, 1, 0) != 0)
        {
            return Task.CompletedTask;
        }

        delayedPumpCancelSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        delayedPumpTask = StartDelayedMessagePump(delayedPumpCancelSource.Token);
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
    readonly TimeProvider timeProvider;
    readonly InMemoryBrokerOptions options;
    readonly ConcurrentDictionary<(InMemorySimulationOperation Operation, string Queue), WindowState> simulationState = new();

    sealed class WindowState(DateTimeOffset windowStart)
    {
        public DateTimeOffset WindowStart { get; set; } = windowStart;

        public int PermitsUsed { get; set; }
    }

    readonly record struct ResolvedSimulationSettings(TimeProvider TimeProvider, InMemorySimulationMode? Mode, InMemoryRateLimitOptions? RateLimit);

    enum InMemorySimulationOperation
    {
        Send,
        Receive,
        DelayedDelivery
    }
}
