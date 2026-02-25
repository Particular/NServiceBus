#nullable enable

namespace NServiceBus;

using System;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus.Logging;
using NServiceBus.Transport;

class LogWrappedMessageReceiver(IMessageReceiver receiver, object endpointLogSlot) : IMessageReceiver
{
    public ISubscriptionManager Subscriptions => receiver.Subscriptions;
    public string Id => receiver.Id;
    public string ReceiveAddress => receiver.ReceiveAddress;

    public Task ChangeConcurrency(PushRuntimeSettings limitations, CancellationToken cancellationToken = default) =>
        receiver.ChangeConcurrency(limitations, cancellationToken);

    public Task StartReceive(CancellationToken cancellationToken = default) =>
        receiver.StartReceive(cancellationToken);

    public Task StopReceive(CancellationToken cancellationToken = default) =>
        receiver.StopReceive(cancellationToken);

    public Task Initialize(PushRuntimeSettings limitations, OnMessage onMessage, OnError onError, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(onMessage);
        ArgumentNullException.ThrowIfNull(onError);

        return receiver.Initialize(limitations, ScopedOnMessage, ScopedOnError, cancellationToken);

        async Task ScopedOnMessage(MessageContext messageContext, CancellationToken ct)
        {
            using var _ = LogManager.BeginSlotScope(endpointLogSlot);
            await onMessage(messageContext, ct).ConfigureAwait(false);
        }

        async Task<ErrorHandleResult> ScopedOnError(ErrorContext errorContext, CancellationToken ct)
        {
            using var _ = LogManager.BeginSlotScope(endpointLogSlot);
            return await onError(errorContext, ct).ConfigureAwait(false);
        }
    }
}