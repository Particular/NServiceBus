namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using DelayedDelivery;
    using DeliveryConstraints;
    using Extensibility;
    using Routing;
    using Transport;

    class DelayedRetryExecutor
    {
        public DelayedRetryExecutor(string endpointInputQueue, IDispatchMessages dispatcher)
        {
            this.dispatcher = dispatcher;
            this.endpointInputQueue = endpointInputQueue;
        }

        public async Task<int> Retry(IncomingMessage message, TimeSpan delay, TransportTransaction transportTransaction)
        {
            var outgoingMessage = new OutgoingMessage(message.MessageId, new Dictionary<string, string>(message.Headers), message.Body);

            var currentDelayedRetriesAttempt = message.GetDelayedDeliveriesPerformed() + 1;

            outgoingMessage.SetCurrentDelayedDeliveries(currentDelayedRetriesAttempt);
            outgoingMessage.SetDelayedDeliveryTimestamp(DateTimeOffset.UtcNow);

            var deliveryConstraints = new List<DeliveryConstraint>(1) { new DelayDeliveryWith(delay) };
            var messageDestination = new UnicastAddressTag(endpointInputQueue);

            var transportOperations = new TransportOperations(new TransportOperation(outgoingMessage, messageDestination, deliveryConstraints: deliveryConstraints));

            await dispatcher.Dispatch(transportOperations, transportTransaction, new ContextBag()).ConfigureAwait(false);

            return currentDelayedRetriesAttempt;
        }

        IDispatchMessages dispatcher;
        string endpointInputQueue;
    }
}