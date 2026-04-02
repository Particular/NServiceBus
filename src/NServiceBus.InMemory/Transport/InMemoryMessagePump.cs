namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Transport;

class InMemoryMessagePump(
    string id,
    string receiveAddress,
    ReceiveSettings receiveSettings,
    TransportTransactionMode transactionMode,
    Action<string, Exception, CancellationToken> criticalErrorAction,
    InMemoryBroker broker) : IMessageReceiver
{
    public string Id { get; } = id;

    public string ReceiveAddress { get; } = receiveAddress;

    public ISubscriptionManager? Subscriptions { get; private set; }

    public ReceiveSettings ReceiveSettings { get; } = receiveSettings;

    internal InlineExecutionRunner Runner { get; } = new(receiveAddress, transactionMode, criticalErrorAction, broker, static () => CancellationToken.None);

    public void ConfigureSubscriptionManager(ISubscriptionManager? subscriptionManager) => Subscriptions = subscriptionManager;

    public Task Initialize(PushRuntimeSettings limitations, OnMessage onMessage, OnError onError, CancellationToken cancellationToken = default)
    {
        Runner.UpdateProcessingCancellationTokenAccessor(() => messageProcessingCts?.Token ?? CancellationToken.None);
        Runner.Initialize(onMessage, onError);
        Runner.SetPump(this);
        pushRuntimeSettings = limitations;
        return Task.CompletedTask;
    }

    public Task StartReceive(CancellationToken cancellationToken = default)
    {
        pumpCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        messageProcessingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        receiveAttemptCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        stopRequested = CreateStopSignal();

        _ = broker.StartPump(cancellationToken);

        pumpTasks.Clear();
        for (var i = 0; i < pushRuntimeSettings.MaxConcurrency; i++)
        {
            pumpTasks.Add(Task.Run(() => PumpMessagesAsync(pumpCts.Token), pumpCts.Token));
        }

        return Task.CompletedTask;
    }

    async Task PumpMessagesAsync(CancellationToken cancellationToken)
    {
        var queue = broker.GetOrCreateQueue(ReceiveAddress);
        BrokerEnvelope? envelope = null;
        var isProcessing = false;

        while (!cancellationToken.IsCancellationRequested)
        {
#pragma warning disable PS0021 // Pump shutdown uses separate tokens: one for graceful receive-stop and one for forced in-flight cancellation.
            try
            {
                if (stopRequested.Task.IsCompleted)
                {
                    if (!queue.TryDequeue(out envelope))
                    {
                        break;
                    }
                }
                else
                {
                    await broker.SimulateReceiveAsync(ReceiveAddress, ReceiveAttemptCancellationToken).ConfigureAwait(false);
                    envelope = await DequeueForProcessingAsync(queue, ReceiveAttemptCancellationToken).ConfigureAwait(false);
                    if (envelope == null)
                    {
                        break;
                    }
                }

                ArgumentNullException.ThrowIfNull(envelope);

                if (IsExpired(envelope, broker.GetCurrentTime()))
                {
                    TryFaultScopeFromEnvelope(envelope, new InvalidOperationException($"Inline execution scope '{envelope.InlineState?.Scope.RootExecutionId}' expired before the message could be received."));
                    envelope.Dispose();
                    envelope = null;
                    continue;
                }

                isProcessing = true;

                var inlineState = envelope.InlineState;
                if (inlineState != null)
                {
                    if (TryTakePendingInlineScopeForProcessing(inlineState.Scope.RootExecutionId, out var existingScope) && existingScope != null && !existingScope.Completion.IsCompleted)
                    {
                        envelope = envelope with { InlineState = new InlineEnvelopeState(existingScope, inlineState.Depth, inlineState.IsRootDispatch) };
                    }
                    else if (existingScope == null || existingScope.Completion.IsCompleted)
                    {
                        // Scope was already completed or removed - process without inline semantics
                        envelope = envelope with { InlineState = null };
                    }
                }

                await Runner.Process(envelope, cancellationToken).ConfigureAwait(false);

                isProcessing = false;
                envelope = null;
            }
            catch (InMemorySimulationException ex)
            {
                if (stopRequested.Task.IsCompleted)
                {
                    continue;
                }

                if (ex.RetryAfter > TimeSpan.Zero)
                {
                    if (!await WaitForRetryOrStopAsync(ex.RetryAfter, ex.TimeProvider, cancellationToken).ConfigureAwait(false))
                    {
                        continue;
                    }
                }
                else
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await Task.Yield();
                }
            }
#pragma warning disable PS0020 // This pump intentionally distinguishes hard-stop cancellation from graceful receive-stop cancellation.
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                if (isProcessing && envelope != null)
                {
                    var inlineState = envelope.InlineState;
                    if (inlineState != null && !inlineState.Scope.Completion.IsCompleted)
                    {
                        RequeuePendingInlineScope(inlineState.Scope);
                    }

                    var retryQueue = broker.GetOrCreateQueue(ReceiveAddress);
                    await retryQueue.Enqueue(envelope, CancellationToken.None).ConfigureAwait(false);
                }
                break;
            }
            catch (OperationCanceledException) when (ReceiveAttemptCancellationRequested)
            {
                continue;
            }
#pragma warning restore PS0020
#pragma warning restore PS0021
        }
    }

    static bool IsExpired(BrokerEnvelope envelope, DateTimeOffset now) =>
        envelope.DiscardAfter.HasValue && envelope.DiscardAfter.Value < now;

    public async Task StopReceive(CancellationToken cancellationToken = default)
    {
        // Graceful stop stops waiting for new work and lets buffered/in-flight processing drain.
        // Only host-forced cancellation escalates to a hard stop that interrupts handlers.
        stopRequested.TrySetResult();
        Cancel(receiveAttemptCts);

        using var cancellationRegistration = cancellationToken.Register(static state => ((InMemoryMessagePump)state!).TriggerHardStop(), this);
        if (cancellationToken.IsCancellationRequested)
        {
            TriggerHardStop();
        }

        if (pumpTasks.Count != 0)
        {
            try
            {
                await Task.WhenAll(pumpTasks).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (pumpCts?.IsCancellationRequested == true || cancellationToken.IsCancellationRequested)
            {
            }
            finally
            {
                pumpTasks.Clear();
                pumpCts?.Dispose();
                pumpCts = null;
                messageProcessingCts?.Dispose();
                messageProcessingCts = null;
                receiveAttemptCts?.Dispose();
                receiveAttemptCts = null;
            }
        }

        if (cancellationToken.IsCancellationRequested)
        {
            await FaultAndDrainRemainingScopesAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }

    public async Task ChangeConcurrency(PushRuntimeSettings limitations, CancellationToken cancellationToken = default)
    {
        await StopReceive(cancellationToken).ConfigureAwait(false);
        pushRuntimeSettings = limitations;
        await StartReceive(cancellationToken).ConfigureAwait(false);
    }

    public void TrackPendingInlineScope(InlineExecutionScope scope)
    {
        lock (activeScopesLock)
        {
            activeScopes[scope.RootExecutionId] = scope;
        }

        _ = scope.Completion.ContinueWith(
            _ => StopTrackingInlineScope(scope.RootExecutionId),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    bool TryTakePendingInlineScopeForProcessing(Guid rootExecutionId, out InlineExecutionScope? scope)
    {
        lock (activeScopesLock)
        {
            var found = activeScopes.TryGetValue(rootExecutionId, out scope);
            if (found)
            {
                activeScopes.Remove(rootExecutionId);
            }

            return found;
        }
    }

    void RequeuePendingInlineScope(InlineExecutionScope scope)
    {
        TrackPendingInlineScope(scope);
    }

    void StopTrackingInlineScope(Guid rootExecutionId)
    {
        lock (activeScopesLock)
        {
            activeScopes.Remove(rootExecutionId);
        }
    }

    void TryFaultScopeFromEnvelope(BrokerEnvelope envelope, Exception exception)
    {
        var inlineState = envelope.InlineState;
        if (inlineState == null)
        {
            return;
        }

        lock (activeScopesLock)
        {
            if (activeScopes.TryGetValue(inlineState.Scope.RootExecutionId, out var scope) && !scope.Completion.IsCompleted)
            {
                scope.CompleteDispatchFailure(exception);
            }
        }
    }

    async Task FaultAndDrainRemainingScopesAsync(CancellationToken cancellationToken)
    {
        List<InlineExecutionScope> scopesToDrain;
        lock (activeScopesLock)
        {
            scopesToDrain = [.. activeScopes.Values];
        }

        foreach (var scope in scopesToDrain)
        {
            scope.CompleteDispatchFailure(new OperationCanceledException($"Inline execution scope '{scope.RootExecutionId}' was faulted because the message pump stopped."));
        }

        await Task.WhenAll(scopesToDrain.Select(scope => AwaitCompletionIgnoringFailure(scope, cancellationToken))).ConfigureAwait(false);

        lock (activeScopesLock)
        {
            activeScopes.Clear();
        }
    }

    static async Task AwaitCompletionIgnoringFailure(InlineExecutionScope scope, CancellationToken cancellationToken)
    {
        try
        {
            await scope.Completion.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch when (scope.Completion.IsCompleted)
        {
        }
    }

    static async ValueTask<BrokerEnvelope?> DequeueForProcessingAsync(InMemoryChannel queue, CancellationToken cancellationToken)
    {
        while (await queue.WaitToRead(cancellationToken).ConfigureAwait(false))
        {
            if (queue.TryDequeue(out var envelope))
            {
                return envelope;
            }
        }

        return null;
    }

    async Task<bool> WaitForRetryOrStopAsync(TimeSpan retryAfter, TimeProvider timeProvider, CancellationToken cancellationToken)
    {
        var delayTask = Task.Delay(retryAfter, timeProvider, cancellationToken);
        var completedTask = await Task.WhenAny(delayTask, stopRequested.Task).ConfigureAwait(false);
        if (completedTask == stopRequested.Task)
        {
            return false;
        }

        await delayTask.ConfigureAwait(false);
        return true;
    }

    void TriggerHardStop()
    {
        Cancel(pumpCts);
        Cancel(messageProcessingCts);
        Cancel(receiveAttemptCts);
    }

    static TaskCompletionSource CreateStopSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    static void Cancel(CancellationTokenSource? cts)
    {
        if (cts is { IsCancellationRequested: false })
        {
            cts.Cancel();
        }
    }

    CancellationToken ReceiveAttemptCancellationToken => receiveAttemptCts?.Token ?? CancellationToken.None;

    bool ReceiveAttemptCancellationRequested => receiveAttemptCts?.IsCancellationRequested == true && pumpCts?.IsCancellationRequested != true;

    PushRuntimeSettings pushRuntimeSettings = null!;
    CancellationTokenSource? pumpCts;
    CancellationTokenSource? messageProcessingCts;
    CancellationTokenSource? receiveAttemptCts;
    TaskCompletionSource stopRequested = CreateStopSignal();
    readonly List<Task> pumpTasks = [];
    readonly Dictionary<Guid, InlineExecutionScope> activeScopes = [];
    readonly Lock activeScopesLock = new();
}
