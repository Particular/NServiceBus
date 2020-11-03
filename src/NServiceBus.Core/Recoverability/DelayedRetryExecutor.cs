using System.Threading;

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
        public DelayedRetryExecutor(string endpointInputQueue, IMessageDispatcher dispatcher, string timeoutManagerAddress = null)
        {
            this.timeoutManagerAddress = timeoutManagerAddress;
            this.dispatcher = dispatcher;
            this.endpointInputQueue = endpointInputQueue;
        }

        public async Task<int> Retry(IncomingMessage message, TimeSpan delay, TransportTransaction transportTransaction)
        {
            var outgoingMessage = new OutgoingMessage(message.MessageId, new Dictionary<string, string>(message.Headers), message.Body);

            var currentDelayedRetriesAttempt = message.GetDelayedDeliveriesPerformed() + 1;

            outgoingMessage.SetCurrentDelayedDeliveries(currentDelayedRetriesAttempt);
            outgoingMessage.SetDelayedDeliveryTimestamp(DateTime.UtcNow);

            UnicastAddressTag messageDestination;
            Dictionary<string, string> properties = new Dictionary<string, string>
            {
                {typeof(DelayDeliveryWith).FullName, delay.ToString("c")}
            };
            // transport supports native deferred messages, directly send to input queue with delay constraint:
            messageDestination = new UnicastAddressTag(endpointInputQueue);

            var transportOperations = new TransportOperations(new TransportOperation(outgoingMessage, messageDestination, properties));

            await dispatcher.Dispatch(transportOperations, transportTransaction, CancellationToken.None).ConfigureAwait(false);

            return currentDelayedRetriesAttempt;
        }

        IMessageDispatcher dispatcher;
        string endpointInputQueue;
        string timeoutManagerAddress;
    }
}