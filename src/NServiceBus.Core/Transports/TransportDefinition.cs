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
        /// Initializes all the factories and supported features for the transport. This method is called right before all features are activated and the settings will be locked down. This means you can use the SettingsHolder both for providing default capabilities as well as for initializing the transport's configuration based on those settings (the user cannot provide information anymore at this stage).
        /// </summary>
        /// <param name="settings">An instance of the current settings.</param>
        /// <returns>The supported factories.</returns>
        protected internal abstract TransportInfrastructure Initialize(SettingsHolder settings);
    }

    /// <summary>
    /// Transport infrastructure definitions.
    /// </summary>
    public abstract class TransportInfrastructure
    {
        /// <summary>
        /// Gets the factories to receive message.
        /// </summary>
        public abstract TransportReceiveInfrastructure ConfigureReceiveInfrastructure(string connectionString);

        /// <summary>
        /// Gets the factories to send message.
        /// </summary>
        public abstract TransportSendInfrastructure ConfigureSendInfrastructure(string connectionString);

        /// <summary>
        /// Gets the factory to manage subscriptions.
        /// </summary>
        public abstract TransportSubscriptionInfrastructure ConfigureSubscriptionInfrastructure();

        /// <summary>
        /// Returns the list of supported delivery constraints for this transport.
        /// </summary>
        public abstract IEnumerable<Type> DeliveryConstraints { get; }

        /// <summary>
        /// Gets the highest supported transaction mode for the this transport.
        /// </summary>
        public abstract TransportTransactionMode TransactionMode { get; }

        /// <summary>
        /// Returns the outbound routing policy selected for the transport.
        /// </summary>
        public abstract OutboundRoutingPolicy OutboundRoutingPolicy { get; }

        /// <summary>
        /// Gets an example connection string to use when reporting lack of configured connection string to the user.
        /// </summary>
        public abstract string ExampleConnectionStringForErrorMessage { get; }

        /// <summary>
        /// Used by implementations to control if a connection string is necessary.
        /// </summary>
        public virtual bool RequiresConnectionString => true;

        /// <summary>
        /// True if the transport.
        /// </summary>
        public bool RequireOutboxConsent { get; protected set; }

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
        /// Returns the canonical for of the given transport address so various transport addresses can be effectively compared and de-duplicated.
        /// </summary>
        /// <param name="transportAddress">A transport address.</param>
        public virtual string MakeCanonicalForm(string transportAddress)
        {
            return transportAddress;
        }
    }
}