namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Extensibility;
using Transport;

class InMemoryMessagePump : IMessageReceiver
{
    public InMemoryMessagePump(
        string id,
        string receiveAddress,
        ReceiveSettings receiveSettings,
        TransportTransactionMode transactionMode,
        InMemoryBroker Broker)
    {
        Id = id;
        ReceiveAddress = receiveAddress;
        ReceiveSettings = receiveSettings;
        TransactionMode = transactionMode;
        this.Broker = Broker;
    }

    public string Id { get; }

    public string ReceiveAddress { get; }

    public ISubscriptionManager? Subscriptions { get; private set; }

    public ReceiveSettings ReceiveSettings { get; }

    TransportTransactionMode TransactionMode { get; }

    InMemoryBroker Broker { get; }

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
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.Token.Register(() => pumpCts?.Cancel());

        pumpCts = cts;

        _ = Broker.StartPump(cancellationToken);

        pumpTasks.Clear();
        for (var i = 0; i < pushRuntimeSettings.MaxConcurrency; i++)
        {
            pumpTasks.Add(Task.Run(() => PumpMessagesAsync(pumpCts.Token), pumpCts.Token));
        }

        return Task.CompletedTask;
    }

    async Task PumpMessagesAsync(CancellationToken cancellationToken)
    {
        var queue = Broker.GetOrCreateQueue(ReceiveAddress);
        var contextBag = new ContextBag();
        BrokerEnvelope? envelope = null;
        var isProcessing = false;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                envelope = await queue.Dequeue(cancellationToken).ConfigureAwait(false);

                if (IsExpired(envelope))
                {
                    envelope.Dispose();
                    envelope = null;
                    continue;
                }

                isProcessing = true;

                var headers = new Dictionary<string, string>(envelope.Headers);

                var transportTransaction = new TransportTransaction();
                InMemoryReceiveTransaction? receiveTransaction = null;

                if (TransactionMode == TransportTransactionMode.SendsAtomicWithReceive)
                {
                    receiveTransaction = new InMemoryReceiveTransaction();
                    transportTransaction.Set<IInMemoryReceiveTransaction>(receiveTransaction);
                }

                var messageContext = new MessageContext(
                    envelope.MessageId,
                    headers,
                    envelope.Body,
                    transportTransaction,
                    ReceiveAddress,
                    contextBag);

                try
                {
                    await onMessage(messageContext, CancellationToken.None).ConfigureAwait(false);

                    if (receiveTransaction != null)
                    {
                        receiveTransaction.Commit();
                        await CommitPendingToBrokerAsync(receiveTransaction, cancellationToken).ConfigureAwait(false);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
                {
                    receiveTransaction?.Rollback();

                    ErrorContext errorContext;

                    if (receiveTransaction != null)
                    {
                        errorContext = new ErrorContext(
                            ex,
                            new Dictionary<string, string>(envelope!.Headers),
                            envelope.MessageId,
                            envelope.Body,
                            transportTransaction,
                            envelope.DeliveryAttempt,
                            ReceiveAddress,
                            contextBag);
                    }
                    else
                    {
                        errorContext = new ErrorContext(
                            ex,
                            new Dictionary<string, string>(envelope!.Headers),
                            envelope.MessageId,
                            envelope.Body,
                            new TransportTransaction(),
                            envelope.DeliveryAttempt,
                            ReceiveAddress,
                            contextBag);
                    }

                    var result = await onError(errorContext, CancellationToken.None).ConfigureAwait(false);

                    if (receiveTransaction != null)
                    {
                        receiveTransaction.Commit();
                        await CommitPendingToBrokerAsync(receiveTransaction, cancellationToken).ConfigureAwait(false);
                    }

                    if (result == ErrorHandleResult.RetryRequired)
                    {
                        var retryEnvelope = envelope!.WithDeliveryAttempt(envelope.DeliveryAttempt + 1);
                        var retryQueue = Broker.GetOrCreateQueue(ReceiveAddress);
                        await retryQueue.Enqueue(retryEnvelope, CancellationToken.None).ConfigureAwait(false);
                    }
                    else
                    {
                        envelope!.Dispose();
                    }
                }

                isProcessing = false;
                envelope = null;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                if (isProcessing && envelope != null)
                {
                    var retryQueue = Broker.GetOrCreateQueue(ReceiveAddress);
                    await retryQueue.Enqueue(envelope, CancellationToken.None).ConfigureAwait(false);
                }
                break;
            }
        }
    }

    async Task CommitPendingToBrokerAsync(InMemoryReceiveTransaction receiveTransaction, CancellationToken cancellationToken)
    {
        var envelopes = receiveTransaction.GetPendingAndClear();
        foreach (var envelope in envelopes)
        {
            var queue = Broker.GetOrCreateQueue(envelope.Destination);
            await queue.Enqueue(envelope, cancellationToken).ConfigureAwait(false);
        }
    }

    static bool IsExpired(BrokerEnvelope envelope) =>
        envelope.DiscardAfter.HasValue && envelope.DiscardAfter.Value < DateTimeOffset.UtcNow;

    public async Task StopReceive(CancellationToken cancellationToken = default)
    {
        pumpCts?.Cancel();

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
            }
        }
    }

    public async Task ChangeConcurrency(PushRuntimeSettings limitations, CancellationToken cancellationToken = default)
    {
        await StopReceive(cancellationToken).ConfigureAwait(false);
        pushRuntimeSettings = limitations;
        await StartReceive(cancellationToken).ConfigureAwait(false);
    }

    OnMessage onMessage = null!;
    OnError onError = null!;
    PushRuntimeSettings pushRuntimeSettings = null!;
    CancellationTokenSource? pumpCts;
    List<Task> pumpTasks = [];
}
