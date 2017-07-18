﻿namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using DelayedDelivery;
    using DeliveryConstraints;
    using Pipeline;
    using Routing;
    using Transport;

    class DelayedRetryExecutor
    {
        public DelayedRetryExecutor(string endpointInputQueue, string timeoutManagerAddress = null)
        {
            this.timeoutManagerAddress = timeoutManagerAddress;
            this.endpointInputQueue = endpointInputQueue;
        }

        public async Task<int> Retry(IncomingMessage message, TimeSpan delay, ErrorContext context)
        {
            var outgoingMessage = new OutgoingMessage(message.MessageId, new Dictionary<string, string>(message.Headers), message.Body);

            var currentDelayedRetriesAttempt = message.GetDelayedDeliveriesPerformed() + 1;

            outgoingMessage.SetCurrentDelayedDeliveries(currentDelayedRetriesAttempt);
            outgoingMessage.SetDelayedDeliveryTimestamp(DateTime.UtcNow);

            UnicastAddressTag messageDestination;
            List<DeliveryConstraint> deliveryConstraints = null;
            if (timeoutManagerAddress == null)
            {
                // transport supports native deferred messages, directly send to input queue with delay constraint:
                deliveryConstraints = new List<DeliveryConstraint>(1)
                {
                    new DelayDeliveryWith(delay)
                };
                messageDestination = new UnicastAddressTag(endpointInputQueue);
            }
            else
            {
                // transport doesn't support native deferred messages, reroute to timeout manager:
                outgoingMessage.Headers[TimeoutManagerHeaders.RouteExpiredTimeoutTo] = endpointInputQueue;
                outgoingMessage.Headers[TimeoutManagerHeaders.Expire] = DateTimeExtensions.ToWireFormattedString(DateTime.UtcNow + delay);
                messageDestination = new UnicastAddressTag(timeoutManagerAddress);
            }

            await context.Dispatch(new TransportOperation(outgoingMessage, messageDestination, deliveryConstraints: deliveryConstraints)).ConfigureAwait(false);

            return currentDelayedRetriesAttempt;
        }
        
        string endpointInputQueue;
        string timeoutManagerAddress;
    }
}