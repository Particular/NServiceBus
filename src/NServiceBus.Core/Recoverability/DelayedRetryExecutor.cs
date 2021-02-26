namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using DelayedDelivery;
    using Routing;
    using Transport;

    class DelayedRetryExecutor
    {
        public DelayedRetryExecutor(string endpointInputQueue, IMessageDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
            this.endpointInputQueue = endpointInputQueue;
        }

        public async Task<int> Retry(IncomingMessage message, TimeSpan delay, TransportTransaction transportTransaction, CancellationToken cancellationToken = default)
        {
            var outgoingMessage = new OutgoingMessage(message.MessageId, new Dictionary<string, string>(message.Headers), message.Body);

            var currentDelayedRetriesAttempt = message.GetDelayedDeliveriesPerformed() + 1;

            outgoingMessage.SetCurrentDelayedDeliveries(currentDelayedRetriesAttempt);
            outgoingMessage.SetDelayedDeliveryTimestamp(DateTimeOffset.UtcNow);

            var dispatchProperties = new DispatchProperties
            {
                DelayDeliveryWith = new DelayDeliveryWith(delay)
            };
            var messageDestination = new UnicastAddressTag(endpointInputQueue);

            var transportOperations = new TransportOperations(new TransportOperation(outgoingMessage, messageDestination, dispatchProperties));

            await dispatcher.Dispatch(transportOperations, transportTransaction, cancellationToken).ConfigureAwait(false);

            return currentDelayedRetriesAttempt;
        }

        IMessageDispatcher dispatcher;
        string endpointInputQueue;
    }
}
