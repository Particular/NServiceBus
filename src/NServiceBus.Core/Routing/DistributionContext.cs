namespace NServiceBus.Routing
{
    using System;
    using System.Collections.Generic;
    using Extensibility;
    using Pipeline;

    /// <summary>
    /// The context for custom <see cref="DistributionStrategy" /> implementations.
    /// </summary>
    public class DistributionContext
    {
        /// <summary>
        /// Creates a new distribution context.
        /// </summary>
        public DistributionContext(string[] receiverAddresses, OutgoingLogicalMessage message, string messageId, Dictionary<string, string> headers, Func<EndpointInstance, string> addressTranslation, ContextBag context)
        {
            this.addressTranslation = addressTranslation;
            ReceiverAddresses = receiverAddresses;
            Message = message;
            MessageId = messageId;
            Headers = headers;
            Context = context;
        }

        /// <summary>
        /// The receiver addresses that can be taken into account for distribution.
        /// </summary>
        public string[] ReceiverAddresses { get; }

        /// <summary>
        /// The id of the outgoing message.
        /// </summary>
        public string MessageId { get; private set; }

        /// <summary>
        /// The headers of the outgoing message.
        /// </summary>
        public Dictionary<string, string> Headers { get; }

        /// <summary>
        /// The outgoing message.
        /// </summary>
        public OutgoingLogicalMessage Message { get; }

        /// <summary>
        /// The context bag.
        /// </summary>
        public ContextBag Context { get; }

        /// <summary>
        /// Converts a given logical address to the transport address.
        /// </summary>
        /// <param name="endpointInstance">The endpoint instance.</param>
        /// <returns>The transport address.</returns>
        public string ToTransportAddress(EndpointInstance endpointInstance)
        {
            return addressTranslation(endpointInstance);
        }

        Func<EndpointInstance, string> addressTranslation;
    }
}