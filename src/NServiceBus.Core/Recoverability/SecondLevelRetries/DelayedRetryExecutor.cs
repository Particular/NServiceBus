namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using DelayedDelivery;
    using DeliveryConstraints;
    using Extensibility;
    using Routing;
    using Transports;

    class DelayedRetryExecutor
    {
        string timeoutManagerAddress;
        readonly IDispatchMessages dispatcher;
        readonly string endpointInputQueue;

        public DelayedRetryExecutor(string endpointInputQueue, IDispatchMessages dispatcher, string timeoutManagerAddress = null)
        {
            this.timeoutManagerAddress = timeoutManagerAddress;
            this.dispatcher = dispatcher;
            this.endpointInputQueue = endpointInputQueue;
        }

        public Task Retry(IncomingMessage message, TimeSpan delay, ContextBag context)
        {
            message.RevertToOriginalBodyIfNeeded();

            var outgoingMessage = new OutgoingMessage(message.MessageId, new Dictionary<string, string>(message.Headers), message.Body);

            var previousRetries = GetNumberOfRetries(message.Headers);
            outgoingMessage.Headers[Headers.Retries] = (previousRetries + 1).ToString();
            outgoingMessage.Headers[Headers.RetriesTimestamp] = DateTimeExtensions.ToWireFormattedString(DateTime.UtcNow);

            UnicastAddressTag messageDestination;
            DeliveryConstraint[] deliveryConstraints = null;
            if (timeoutManagerAddress == null)
            {
                // transport supports native deferred messages, directly send to input queue with delay constraint:
                deliveryConstraints = new DeliveryConstraint[] { new DelayDeliveryWith(delay)};
                messageDestination = new UnicastAddressTag(endpointInputQueue);
            }
            else
            {
                // transport doesn't support native deferred messages, reroute to timeout manager:
                outgoingMessage.Headers[TimeoutManagerHeaders.RouteExpiredTimeoutTo] = endpointInputQueue;
                outgoingMessage.Headers[TimeoutManagerHeaders.Expire] = DateTimeExtensions.ToWireFormattedString(DateTime.UtcNow + delay);
                messageDestination = new UnicastAddressTag(timeoutManagerAddress);
            }

            var transportOperations = new TransportOperations(new TransportOperation(outgoingMessage, messageDestination, deliveryConstraints: deliveryConstraints));
            return dispatcher.Dispatch(transportOperations, context);
        }

        static int GetNumberOfRetries(Dictionary<string, string> headers)
        {
            string value;
            if (headers.TryGetValue(Headers.Retries, out value))
            {
                int i;
                if (int.TryParse(value, out i))
                {
                    return i;
                }
            }
            return 0;
        }
    }
}