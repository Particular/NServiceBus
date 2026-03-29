namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Extensibility;
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

    public void ConfigureSubscriptionManager(ISubscriptionManager? subscriptionManager)
    {
        Subscriptions = subscriptionManager;
    }

    public Task Initialize(PushRuntimeSettings limitations, OnMessage onMessage, OnError onError, CancellationToken cancellationToken = default)
    {
        this.onMessage = onMessage;
        this.onError = onError;
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

                if (IsExpired(envelope))
                {
                    envelope.Dispose();
                    envelope = null;
                    continue;
                }

                isProcessing = true;

                await ProcessEnvelopeAsync(envelope, cancellationToken).ConfigureAwait(false);

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
                    await Task.Yield();
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                if (isProcessing && envelope != null)
                {
                    var retryQueue = broker.GetOrCreateQueue(ReceiveAddress);
                    await retryQueue.Enqueue(envelope, CancellationToken.None).ConfigureAwait(false);
                }
                break;
            }
        }
    }

    async Task ProcessEnvelopeAsync(BrokerEnvelope envelope, CancellationToken pumpCancellationToken)
    {
        var headers = new Dictionary<string, string>(envelope.Headers);

        var transportTransaction = new TransportTransaction();
        InMemoryReceiveTransaction? receiveTransaction = null;

        if (transactionMode == TransportTransactionMode.SendsAtomicWithReceive)
        {
            receiveTransaction = new InMemoryReceiveTransaction();
            transportTransaction.Set<IInMemoryReceiveTransaction>(receiveTransaction);
            transportTransaction.Set(receiveTransaction.StorageTransaction);
        }

        var contextBag = new ContextBag();

        var messageContext = new MessageContext(
            envelope.MessageId,
            headers,
            envelope.Body,
            transportTransaction,
            ReceiveAddress,
            contextBag);

        try
        {
            await onMessage(messageContext, ProcessingCancellationToken).ConfigureAwait(false);

            if (receiveTransaction != null)
            {
                receiveTransaction.Commit();
                await CommitPendingToBrokerAsync(receiveTransaction, ProcessingCancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ProcessingCancellationToken.IsCancellationRequested)
        {
            receiveTransaction?.Rollback();

            var errorContext = receiveTransaction != null
                ? new ErrorContext(
                    ex,
                    new Dictionary<string, string>(envelope.Headers),
                    envelope.MessageId,
                    envelope.Body,
                    transportTransaction,
                    envelope.DeliveryAttempt,
                    ReceiveAddress,
                    contextBag)
                : new ErrorContext(
                    ex,
                    new Dictionary<string, string>(envelope.Headers),
                    envelope.MessageId,
                    envelope.Body,
                    new TransportTransaction(),
                    envelope.DeliveryAttempt,
                    ReceiveAddress,
                    contextBag);

            var result = await HandleErrorAsync(errorContext, messageContext, receiveTransaction, pumpCancellationToken).ConfigureAwait(false);

            if (receiveTransaction != null)
            {
                receiveTransaction.Commit();
                await CommitPendingToBrokerAsync(receiveTransaction, ProcessingCancellationToken).ConfigureAwait(false);
            }

            if (result == ErrorHandleResult.RetryRequired)
            {
                var retryEnvelope = envelope.WithDeliveryAttempt(envelope.DeliveryAttempt + 1);
                var retryQueue = broker.GetOrCreateQueue(ReceiveAddress);
                await retryQueue.Enqueue(retryEnvelope, CancellationToken.None).ConfigureAwait(false);
                return;
            }

            envelope.Dispose();
        }
    }

    async Task<ErrorHandleResult> HandleErrorAsync(
        ErrorContext errorContext,
        MessageContext messageContext,
        InMemoryReceiveTransaction? receiveTransaction,
        CancellationToken pumpCancellationToken)
    {
        try
        {
            return await onError(errorContext, ProcessingCancellationToken).ConfigureAwait(false);
        }
        catch (Exception onErrorException) when (onErrorException is not OperationCanceledException || !ProcessingCancellationToken.IsCancellationRequested)
        {
            receiveTransaction?.Rollback();
            criticalErrorAction($"Failed to execute recoverability policy for message with native ID: `{messageContext.NativeMessageId}`", onErrorException, pumpCancellationToken);
            return ErrorHandleResult.RetryRequired;
        }
    }

    async Task CommitPendingToBrokerAsync(InMemoryReceiveTransaction receiveTransaction, CancellationToken cancellationToken)
    {
        var envelopes = receiveTransaction.GetPendingAndClear();
        foreach (var envelope in envelopes)
        {
            var queue = broker.GetOrCreateQueue(envelope.Destination);
            await queue.Enqueue(envelope, cancellationToken).ConfigureAwait(false);
        }
    }

    static bool IsExpired(BrokerEnvelope envelope) =>
        envelope.DiscardAfter.HasValue && envelope.DiscardAfter.Value < DateTimeOffset.UtcNow;

    public async Task StopReceive(CancellationToken cancellationToken = default)
    {
        pumpCts?.Cancel();

        if (cancellationToken.IsCancellationRequested)
        {
            messageProcessingCts?.Cancel();
        }

        if (pumpTasks.Count != 0)
        {
            try
            {
                await Task.WhenAll(pumpTasks).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
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
    }

    public async Task ChangeConcurrency(PushRuntimeSettings limitations, CancellationToken cancellationToken = default)
    {
        await StopReceive(cancellationToken).ConfigureAwait(false);
        pushRuntimeSettings = limitations;
        await StartReceive(cancellationToken).ConfigureAwait(false);
    }

    CancellationToken ProcessingCancellationToken => messageProcessingCts?.Token ?? CancellationToken.None;

    OnMessage onMessage = null!;
    OnError onError = null!;
    PushRuntimeSettings pushRuntimeSettings = null!;
    CancellationTokenSource? pumpCts;
    CancellationTokenSource? messageProcessingCts;
    readonly List<Task> pumpTasks = [];
}
