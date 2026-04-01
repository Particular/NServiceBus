namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
            try
            {
                await broker.SimulateReceiveAsync(ReceiveAddress, cancellationToken).ConfigureAwait(false);
                envelope = await queue.Dequeue(cancellationToken).ConfigureAwait(false);

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
                    if (TryGetInlineScope(inlineState.Scope.RootExecutionId, out var existingScope) && existingScope != null && !existingScope.Completion.IsCompleted)
                    {
                        envelope = envelope with { InlineState = new InlineEnvelopeState(existingScope, inlineState.Depth, inlineState.IsRootDispatch) };
                        // Remove from activeScopes while processing - will be re-registered if delayed retry
                        RemoveInlineScope(existingScope.RootExecutionId);
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
                if (ex.RetryAfter > TimeSpan.Zero)
                {
                    await Task.Delay(ex.RetryAfter, ex.TimeProvider, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await Task.Yield();
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                if (isProcessing && envelope != null)
                {
                    var inlineState = envelope.InlineState;
                    if (inlineState != null && !inlineState.Scope.Completion.IsCompleted)
                    {
                        RegisterInlineScope(inlineState.Scope);
                    }

                    var retryQueue = broker.GetOrCreateQueue(ReceiveAddress);
                    await retryQueue.Enqueue(envelope, CancellationToken.None).ConfigureAwait(false);
                }
                break;
            }
        }
    }

    static bool IsExpired(BrokerEnvelope envelope, DateTimeOffset now) =>
        envelope.DiscardAfter.HasValue && envelope.DiscardAfter.Value < now;

    public async Task StopReceive(CancellationToken cancellationToken = default)
    {
        if (pumpCts is not null)
        {
            await pumpCts.CancelAsync().ConfigureAwait(false);
        }

        if (cancellationToken.IsCancellationRequested && messageProcessingCts is not null)
        {
            await messageProcessingCts.CancelAsync().ConfigureAwait(false);
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
            }
        }

        await FaultAndDrainRemainingScopesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task ChangeConcurrency(PushRuntimeSettings limitations, CancellationToken cancellationToken = default)
    {
        await StopReceive(cancellationToken).ConfigureAwait(false);
        pushRuntimeSettings = limitations;
        await StartReceive(cancellationToken).ConfigureAwait(false);
    }

    public void RegisterInlineScope(InlineExecutionScope scope)
    {
        lock (activeScopesLock)
        {
            activeScopes[scope.RootExecutionId] = scope;
        }

        _ = scope.Completion.ContinueWith(
            _ => RemoveInlineScope(scope.RootExecutionId),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    bool TryGetInlineScope(Guid rootExecutionId, out InlineExecutionScope? scope)
    {
        lock (activeScopesLock)
        {
            return activeScopes.TryGetValue(rootExecutionId, out scope);
        }
    }

    void RemoveInlineScope(Guid rootExecutionId)
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
                scope.MarkTerminalFailure(exception);
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
            scope.MarkTerminalFailure(new OperationCanceledException($"Inline execution scope '{scope.RootExecutionId}' was faulted because the message pump stopped."));
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

    PushRuntimeSettings pushRuntimeSettings = null!;
    CancellationTokenSource? pumpCts;
    CancellationTokenSource? messageProcessingCts;
    readonly List<Task> pumpTasks = [];
    readonly Dictionary<Guid, InlineExecutionScope> activeScopes = [];
    readonly Lock activeScopesLock = new();
}
