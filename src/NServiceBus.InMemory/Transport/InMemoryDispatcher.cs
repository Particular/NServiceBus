namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Transport;

class InMemoryDispatcher(InMemoryBroker broker) : IMessageDispatcher
{
    public Task Dispatch(TransportOperations outgoingMessages, TransportTransaction transaction, CancellationToken cancellationToken = default) =>
        Task.WhenAll(
            DispatchUnicast(outgoingMessages.UnicastTransportOperations, transaction, cancellationToken),
            DispatchMulticast(outgoingMessages.MulticastTransportOperations, transaction, cancellationToken));

    async Task DispatchMulticast(IEnumerable<MulticastTransportOperation> transportOperations, TransportTransaction transaction, CancellationToken cancellationToken)
    {
        List<Task> tasks = [];

        foreach (var transportOperation in transportOperations)
        {
            var message = transportOperation.Message;
            var messageId = Guid.NewGuid().ToString();
            var sequenceNumber = broker.GetNextSequenceNumber();

            var subscribers = GetSubscribersForType(transportOperation.MessageType);

            foreach (var subscriber in subscribers)
            {
                var now = broker.GetCurrentTime();
                var deliverAt = GetDeliverAt(transportOperation.Properties, now);
                var discardAfter = GetDiscardAfter(transportOperation.Properties, now);

                var envelope = BrokerPayloadStore.Borrow(
                    messageId,
                    message.Body.Span,
                    message.Headers,
                    subscriber,
                    isPublished: true,
                    sequenceNumber,
                    deliverAt,
                    discardAfter);

                if (TryEnlistToReceiveTransaction(transaction, envelope, transportOperation.RequiredDispatchConsistency))
                {
                    continue;
                }

                await broker.SimulateSendAsync(subscriber, cancellationToken).ConfigureAwait(false);

                if (deliverAt.HasValue)
                {
                    broker.EnqueueDelayed(envelope, deliverAt.Value);
                }
                else
                {
                    var queue = broker.GetOrCreateQueue(subscriber);
                    tasks.Add(queue.Enqueue(envelope, cancellationToken).AsTask());
                }
            }
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    HashSet<string> GetSubscribersForType(Type messageType)
    {
        HashSet<string> result = [];
        foreach (var type in GetPotentialEventTypes(messageType))
        {
            foreach (var subscriber in broker.GetSubscribers(type.FullName!))
            {
                result.Add(subscriber);
            }
        }
        return result;
    }

    static HashSet<Type> GetPotentialEventTypes(Type messageType)
    {
        HashSet<Type> allEventTypes = [];
        for (var current = messageType; current != null && !IsCoreMarkerInterface(current); current = current.BaseType)
        {
            allEventTypes.Add(current);
        }
        foreach (var iface in messageType.GetInterfaces())
        {
            if (!IsCoreMarkerInterface(iface))
            {
                allEventTypes.Add(iface);
            }
        }
        return allEventTypes;
    }

    static bool IsCoreMarkerInterface(Type type) =>
        type == typeof(IMessage) || type == typeof(IEvent) || type == typeof(ICommand);

    async Task DispatchUnicast(IEnumerable<UnicastTransportOperation> operations, TransportTransaction transaction, CancellationToken cancellationToken)
    {
        List<Task> tasks = [];

        foreach (var operation in operations)
        {
            var message = operation.Message;
            var messageId = Guid.NewGuid().ToString();
            var sequenceNumber = broker.GetNextSequenceNumber();
            var now = broker.GetCurrentTime();
            var deliverAt = GetDeliverAt(operation.Properties, now);
            var discardAfter = GetDiscardAfter(operation.Properties, now);

            var envelope = BrokerPayloadStore.Borrow(
                messageId,
                message.Body.Span,
                message.Headers,
                operation.Destination,
                isPublished: false,
                sequenceNumber,
                deliverAt,
                discardAfter);

            if (TryEnlistToReceiveTransaction(transaction, envelope, operation.RequiredDispatchConsistency))
            {
                continue;
            }

            await broker.SimulateSendAsync(operation.Destination, cancellationToken).ConfigureAwait(false);

            if (deliverAt.HasValue)
            {
                broker.EnqueueDelayed(envelope, deliverAt.Value);
                continue;
            }

            var queue = broker.GetOrCreateQueue(operation.Destination);
            tasks.Add(queue.Enqueue(envelope, cancellationToken).AsTask());
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    static DateTimeOffset? GetDeliverAt(DispatchProperties properties, DateTimeOffset now)
    {
        if (properties.DoNotDeliverBefore is not null)
        {
            return properties.DoNotDeliverBefore.At.ToUniversalTime();
        }

        return now + properties.DelayDeliveryWith?.Delay;
    }

    static DateTimeOffset? GetDiscardAfter(DispatchProperties properties, DateTimeOffset now)
    {
        var ttbr = properties.DiscardIfNotReceivedBefore;
        if (ttbr != null && ttbr.MaxTime < TimeSpan.MaxValue)
        {
            return now + ttbr.MaxTime;
        }
        return null;
    }

    static bool TryEnlistToReceiveTransaction(TransportTransaction transaction, BrokerEnvelope envelope, DispatchConsistency dispatchConsistency)
    {
        if (dispatchConsistency == DispatchConsistency.Isolated)
        {
            return false;
        }
        if (transaction.TryGet<IInMemoryReceiveTransaction>(out var receiveTransaction) && receiveTransaction != null)
        {
            receiveTransaction.Enlist(envelope);
            return true;
        }
        return false;
    }
}
