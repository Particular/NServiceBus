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

    public ISubscriptionManager Subscriptions { get; private set; } = null!;

    public ReceiveSettings ReceiveSettings { get; }

    TransportTransactionMode TransactionMode { get; }

    InMemoryBroker Broker { get; }

    public void ConfigureSubscriptionManager(ISubscriptionManager subscriptionManager)
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

        for (var i = 0; i < pushRuntimeSettings.MaxConcurrency; i++)
        {
            _ = Task.Run(() => PumpMessagesAsync(pumpCts.Token), pumpCts.Token);
        }

        return Task.CompletedTask;
    }

    async Task PumpMessagesAsync(CancellationToken cancellationToken)
    {
        var queue = Broker.GetOrCreateQueue(ReceiveAddress);
        var contextBag = new ContextBag();

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var envelope = await queue.Dequeue(cancellationToken).ConfigureAwait(false);

                var headers = new Dictionary<string, string>(envelope.Headers);

                var transportTransaction = new TransportTransaction();
                InMemoryReceiveTransaction? receiveTransaction = null;

                if (TransactionMode == TransportTransactionMode.SendsAtomicWithReceive)
                {
                    receiveTransaction = new InMemoryReceiveTransaction();
                    transportTransaction.Set(receiveTransaction);
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
                    throw;
                }
                catch (Exception ex)
                {
                    receiveTransaction?.Rollback();

                    var errorContext = new ErrorContext(
                        ex,
                        [],
                        string.Empty,
                        ReadOnlyMemory<byte>.Empty,
                        new TransportTransaction(),
                        0,
                        ReceiveAddress,
                        contextBag);

                    await onError(errorContext, CancellationToken.None).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
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

    public Task StopReceive(CancellationToken cancellationToken = default)
    {
        pumpCts?.Cancel();
        return Task.CompletedTask;
    }

    public Task ChangeConcurrency(PushRuntimeSettings limitations, CancellationToken cancellationToken = default)
    {
        pushRuntimeSettings = limitations;
        return Task.CompletedTask;
    }

    OnMessage onMessage = null!;
    OnError onError = null!;
    PushRuntimeSettings pushRuntimeSettings = null!;
    CancellationTokenSource? pumpCts;
}
