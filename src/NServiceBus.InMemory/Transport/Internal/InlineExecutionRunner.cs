namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Extensibility;
using Routing;
using Transport;

sealed class InlineExecutionRunner(
    string receiveAddress,
    TransportTransactionMode transactionMode,
    Action<string, Exception, CancellationToken> criticalErrorAction,
    InMemoryBroker broker,
    Func<CancellationToken> processingCancellationTokenAccessor)
{
    public void Initialize(OnMessage onMessage, OnError onError)
    {
        this.onMessage = onMessage;
        this.onError = onError;
    }

    public void SetPump(InMemoryMessagePump pump) => this.pump = pump;

    public void SetDispatcher(IMessageDispatcher dispatcher) => this.dispatcher = dispatcher;

    public void UpdateProcessingCancellationTokenAccessor(Func<CancellationToken> accessor) => processingCancellationTokenAccessor = accessor;

    public async Task Process(BrokerEnvelope envelope, CancellationToken cancellationToken = default)
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

        transportTransaction.Set(ReceivePipelineTransportTransactionMarker.Instance);

        var contextBag = new ContextBag();

        var inlineState = envelope.InlineState;
        if (inlineState != null)
        {
            transportTransaction.Set(inlineState.Scope);
            var dispatchContext = new InlineExecutionDispatchContext(inlineState.Scope, inlineState.Depth);
            transportTransaction.Set(dispatchContext);
            contextBag.Set(dispatchContext);
        }

        var messageContext = new MessageContext(
            envelope.MessageId,
            headers,
            envelope.Body,
            transportTransaction,
            receiveAddress,
            contextBag);

        try
        {
            await onMessage(messageContext, ProcessingCancellationToken).ConfigureAwait(false);

            if (receiveTransaction != null)
            {
                receiveTransaction.Commit();
                await CommitPendingToBrokerAsync(receiveTransaction, ProcessingCancellationToken).ConfigureAwait(false);
            }

            inlineState?.Scope.MarkSuccess();
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
                    receiveAddress,
                    contextBag)
                : new ErrorContext(
                    ex,
                    new Dictionary<string, string>(envelope.Headers),
                    envelope.MessageId,
                    envelope.Body,
                    new TransportTransaction(),
                    envelope.DeliveryAttempt,
                    receiveAddress,
                    contextBag);

            var result = await HandleErrorAsync(errorContext, messageContext, receiveTransaction, cancellationToken).ConfigureAwait(false);

            if (receiveTransaction != null)
            {
                receiveTransaction.Commit();
                await CommitPendingToBrokerAsync(receiveTransaction, ProcessingCancellationToken).ConfigureAwait(false);
            }

            if (result == ErrorHandleResult.RetryRequired)
            {
                if (inlineState != null && pump != null)
                {
                    pump.RegisterInlineScope(inlineState.Scope);
                }

                var retryEnvelope = envelope.WithDeliveryAttempt(envelope.DeliveryAttempt + 1);
                var retryQueue = broker.GetOrCreateQueue(receiveAddress);
                await retryQueue.Enqueue(retryEnvelope, CancellationToken.None).ConfigureAwait(false);
                return;
            }

            inlineState?.Scope.MarkTerminalFailure(ex);
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
        if (dispatcher == null)
        {
            foreach (var envelope in receiveTransaction.GetPendingAndClear())
            {
                var queue = broker.GetOrCreateQueue(envelope.Destination);
                await queue.Enqueue(envelope, cancellationToken).ConfigureAwait(false);
            }
        }
        else
        {
            var envelopes = receiveTransaction.GetPendingAndClear();
            if (envelopes.Count == 0)
            {
                return;
            }

            var operations = new TransportOperation[envelopes.Count];
            for (var i = 0; i < envelopes.Count; i++)
            {
                var pending = envelopes[i];
                operations[i] = new TransportOperation(
                    new OutgoingMessage(pending.MessageId, new Dictionary<string, string>(pending.Headers), pending.Body),
                    new UnicastAddressTag(pending.Destination),
                    [],
                    DispatchConsistency.Default);
            }

            await dispatcher.Dispatch(new TransportOperations(operations), new TransportTransaction(), cancellationToken).ConfigureAwait(false);
        }
    }

    CancellationToken ProcessingCancellationToken => processingCancellationTokenAccessor();

    OnMessage onMessage = null!;
    OnError onError = null!;
    IMessageDispatcher? dispatcher;
    InMemoryMessagePump? pump;
    Func<CancellationToken> processingCancellationTokenAccessor = processingCancellationTokenAccessor;
}