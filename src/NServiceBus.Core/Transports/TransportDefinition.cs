namespace NServiceBus.Transports
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Routing;
    using Settings;

    /// <summary>
    /// Defines a transport.
    /// </summary>
    public abstract partial class TransportDefinition
    {
        /// <summary>
        /// True if the transport.
        /// </summary>
        public bool RequireOutboxConsent { get; set; }

        /// <summary>
        /// Configures transport for receiving.
        /// </summary>
        protected internal abstract TransportReceivingConfigurationResult ConfigureForReceiving(TransportReceivingConfigurationContext context);

        /// <summary>
        /// Configures transport for sending.
        /// </summary>
        protected internal abstract TransportSendingConfigurationResult ConfigureForSending(TransportSendingConfigurationContext context);
        
        /// <summary>
        /// Returns the list of supported delivery constraints for this transport.
        /// </summary>
        public abstract IEnumerable<Type> GetSupportedDeliveryConstraints();

        /// <summary>
        /// Gets the highest supported transaction mode for the this transport.
        /// </summary>
        public abstract TransportTransactionMode GetSupportedTransactionMode();

        /// <summary>
        /// Will be called if the transport has indicated that it has native support for pub sub.
        /// Creates a transport address for the input queue defined by a logical address.
        /// </summary>
        public abstract IManageSubscriptions GetSubscriptionManager();

        /// <summary>
        /// Returns the discriminator for this endpoint instance.
        /// </summary>
        public abstract EndpointInstance BindToLocalEndpoint(EndpointInstance instance, ReadOnlySettings settings);

        /// <summary>
        /// Converts a given logical address to the transport address.
        /// </summary>
        /// <param name="logicalAddress">The logical address.</param>
        /// <returns>The transport address.</returns>
        public abstract string ToTransportAddress(LogicalAddress logicalAddress);

        /// <summary>
        /// Returns the canonical for of the given transport address so various transport addresses can be effectively compared and deduplicated.
        /// </summary>
        public virtual string MakeCanonicalForm(string transportAddress, ReadOnlySettings settings)
        {
            return transportAddress;
        }

        /// <summary>
        /// Returns the outbound routing policy selected for the transport.
        /// </summary>
        public abstract OutboundRoutingPolicy GetOutboundRoutingPolicy(ReadOnlySettings settings);

        /// <summary>
        /// Gets an example connection string to use when reporting lack of configured connection string to the user.
        /// </summary>
        public abstract string ExampleConnectionStringForErrorMessage { get; }

        /// <summary>
        /// Used by implementations to control if a connection string is necessary.
        /// </summary>
        public virtual bool RequiresConnectionString => true;        
    }
}