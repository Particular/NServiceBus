namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Transport;

class InMemoryDispatcher : IMessageDispatcher
{
    public InMemoryDispatcher(InMemoryBroker broker)
    {
        this.broker = broker;
    }

    public Task Dispatch(TransportOperations outgoingMessages, TransportTransaction transaction, CancellationToken cancellationToken = default)
    {
        return Task.WhenAll(
            DispatchUnicast(outgoingMessages.UnicastTransportOperations, transaction, cancellationToken),
            DispatchMulticast(outgoingMessages.MulticastTransportOperations, transaction, cancellationToken));
    }

    async Task DispatchMulticast(IEnumerable<MulticastTransportOperation> transportOperations, TransportTransaction transaction, CancellationToken cancellationToken)
    {
        var tasks = new List<Task>();

        foreach (var transportOperation in transportOperations)
        {
            var message = transportOperation.Message;
            var messageId = Guid.NewGuid().ToString();
            var sequenceNumber = broker.GetNextSequenceNumber();

            var subscribers = GetSubscribersForType(transportOperation.MessageType);

            foreach (var subscriber in subscribers)
            {
                var envelope = BrokerPayloadStore.Borrow(
                    messageId,
                    message.Body.Span,
                    message.Headers,
                    subscriber,
                    isPublished: true,
                    sequenceNumber);

                if (TryEnlistToReceiveTransaction(transaction, envelope, transportOperation.RequiredDispatchConsistency))
                {
                    continue;
                }

                var deliverAt = GetDeliverAt(transportOperation.Properties);
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
        var result = new HashSet<string>();
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
        var allEventTypes = new HashSet<Type>();
        var current = messageType;
        while (current != null)
        {
            if (IsCoreMarkerInterface(current))
            {
                break;
            }
            allEventTypes.Add(current);
            current = current.BaseType;
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

    Task DispatchUnicast(IEnumerable<UnicastTransportOperation> operations, TransportTransaction transaction, CancellationToken cancellationToken)
    {
        return Task.WhenAll(operations.Select(operation =>
        {
            var message = operation.Message;
            var messageId = Guid.NewGuid().ToString();
            var sequenceNumber = broker.GetNextSequenceNumber();

            var envelope = BrokerPayloadStore.Borrow(
                messageId,
                message.Body.Span,
                message.Headers,
                operation.Destination,
                isPublished: false,
                sequenceNumber);

            if (TryEnlistToReceiveTransaction(transaction, envelope, operation.RequiredDispatchConsistency))
            {
                return Task.CompletedTask;
            }

            var deliverAt = GetDeliverAt(operation.Properties);
            if (deliverAt.HasValue)
            {
                broker.EnqueueDelayed(envelope, deliverAt.Value);
                return Task.CompletedTask;
            }

            var queue = broker.GetOrCreateQueue(operation.Destination);
            return queue.Enqueue(envelope, cancellationToken).AsTask();
        }));
    }

    static DateTimeOffset? GetDeliverAt(DispatchProperties properties)
    {
        if (properties.DoNotDeliverBefore != null)
        {
            return properties.DoNotDeliverBefore.At.ToUniversalTime();
        }
        if (properties.DelayDeliveryWith != null)
        {
            return DateTimeOffset.UtcNow + properties.DelayDeliveryWith.Delay;
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

    readonly InMemoryBroker broker;
}
