namespace NServiceBus.Routing
{
    using System.Collections.Generic;
    using Extensibility;
    using Pipeline;
    using Transport;

    /// <summary>
    /// The context for custom <see cref="DistributionStrategy" /> implementations.
    /// </summary>
    public class DistributionContext : IExtendable
    {
        /// <summary>
        /// Creates a new distribution context.
        /// </summary>
        public DistributionContext(string[] receiverAddresses, OutgoingLogicalMessage message, string messageId, Dictionary<string, string> headers, TransportInfrastructure transportInfrastructure, ContextBag extensions)
        {
            this.transportInfrastructure = transportInfrastructure;
            ReceiverAddresses = receiverAddresses;
            Message = message;
            MessageId = messageId;
            Headers = headers;
            Extensions = extensions;
        }

        /// <summary>
        /// The receiver addresses that can be taken into account for distribution.
        /// </summary>
        public string[] ReceiverAddresses { get; }

        /// <summary>
        /// The id of the outgoing message.
        /// </summary>
        public string MessageId { get; }

        /// <summary>
        /// The headers of the outgoing message.
        /// </summary>
        public Dictionary<string, string> Headers { get; }

        /// <summary>
        /// The outgoing message.
        /// </summary>
        public OutgoingLogicalMessage Message { get; }

        /// <summary>
        /// A <see cref="ContextBag" /> which can be used to extend the current object.
        /// </summary>
        public ContextBag Extensions { get; }

        /// <summary>
        /// Converts a given logical address to the transport address.
        /// </summary>
        /// <param name="endpointInstance">The endpoint instance.</param>
        /// <returns>The transport address.</returns>
        public string ToTransportAddress(EndpointInstance endpointInstance)
        {
            Guard.AgainstNull(nameof(endpointInstance), endpointInstance);
            return transportInfrastructure.ToTransportAddress(new QueueAddress(endpointInstance.Endpoint, endpointInstance.Discriminator, endpointInstance.Properties, null));
        }

        TransportInfrastructure transportInfrastructure;
    }
}