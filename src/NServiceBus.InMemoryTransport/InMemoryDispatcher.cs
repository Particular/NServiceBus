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
            var body = broker.PayloadStore.Copy(message.Body.Span);
            var messageId = Guid.NewGuid().ToString();
            var sequenceNumber = broker.GetNextSequenceNumber();

            var subscribers = broker.GetSubscribers(message.Headers[Headers.EnclosedMessageTypes] ?? message.MessageId ?? messageId);

            foreach (var subscriber in subscribers)
            {
                var envelope = BrokerEnvelope.Create(
                    messageId,
                    body,
                    message.Headers,
                    subscriber,
                    isPublished: true,
                    sequenceNumber);

                if (TryEnlistToReceiveTransaction(transaction, envelope))
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

    Task DispatchUnicast(IEnumerable<UnicastTransportOperation> operations, TransportTransaction transaction, CancellationToken cancellationToken)
    {
        return Task.WhenAll(operations.Select(operation =>
        {
            var message = operation.Message;
            var body = broker.PayloadStore.Copy(message.Body.Span);
            var messageId = Guid.NewGuid().ToString();
            var sequenceNumber = broker.GetNextSequenceNumber();

            var envelope = BrokerEnvelope.Create(
                messageId,
                body,
                message.Headers,
                operation.Destination,
                isPublished: false,
                sequenceNumber);

            if (TryEnlistToReceiveTransaction(transaction, envelope))
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

    static bool TryEnlistToReceiveTransaction(TransportTransaction transaction, BrokerEnvelope envelope)
    {
        if (transaction.TryGet<IInMemoryReceiveTransaction>(out var receiveTransaction) && receiveTransaction != null)
        {
            receiveTransaction.Enlist(envelope);
            return true;
        }
        return false;
    }

    readonly InMemoryBroker broker;
}
