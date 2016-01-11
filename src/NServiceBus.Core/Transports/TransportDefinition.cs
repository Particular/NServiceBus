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
        /// Creates a new instance of <see cref="TransportInfrastructure"/>.
        /// </summary>
        /// <param name="configureSendInfrastructure">The factory to create <see cref="IDispatchMessages"/>.</param>
        /// <param name="configureReceiveInfrastructure">The factory to create <see cref="IPushMessages"/>.</param>
        /// <param name="configureSubscriptionInfrastructure">The factory to create <see cref="IManageSubscriptions"/>.</param>
        /// <param name="deliveryConstraints">The delivery constraints.</param>
        /// <param name="transactionMode">The transaction mode.</param>
        /// <param name="outboundRoutingPolicy">The outbound routing policy.</param>
        public TransportInfrastructure(IEnumerable<Type> deliveryConstraints, TransportTransactionMode transactionMode, OutboundRoutingPolicy outboundRoutingPolicy, Func<string, TransportSendInfrastructure> configureSendInfrastructure, Func<string, TransportReceiveInfrastructure> configureReceiveInfrastructure = null, Func<TransportSubscriptionInfrastructure> configureSubscriptionInfrastructure = null)
        {
            DeliveryConstraints = deliveryConstraints;
            TransactionMode = transactionMode;
            OutboundRoutingPolicy = outboundRoutingPolicy;
            ConfigureSubscriptionInfrastructure = configureSubscriptionInfrastructure;
            ConfigureReceiveInfrastructure = configureReceiveInfrastructure;
            ConfigureSendInfrastructure = configureSendInfrastructure;
        }

        /// <summary>
        /// Gets the factories to receive message.
        /// </summary>
        public Func<string, TransportReceiveInfrastructure> ConfigureReceiveInfrastructure { get; }

        /// <summary>
        /// Gets the factories to send message.
        /// </summary>
        public Func<string, TransportSendInfrastructure> ConfigureSendInfrastructure { get; }

        /// <summary>
        /// Gets the factory to manage subscriptions.
        /// </summary>
        public Func<TransportSubscriptionInfrastructure> ConfigureSubscriptionInfrastructure { get; }

        /// <summary>
        /// Returns the list of supported delivery constraints for this transport.
        /// </summary>
        public IEnumerable<Type> DeliveryConstraints { get; }

        /// <summary>
        /// Gets the highest supported transaction mode for the this transport.
        /// </summary>
        public TransportTransactionMode TransactionMode { get; }

        /// <summary>
        /// Returns the outbound routing policy selected for the transport.
        /// </summary>
        public OutboundRoutingPolicy OutboundRoutingPolicy { get; }


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
        /// Returns the canonical for of the given transport address so various transport addresses can be effectively compared and deduplicated.
        /// </summary>
        /// <param name="transportAddress">A transport address.</param>
        public virtual string MakeCanonicalForm(string transportAddress)
        {
            return transportAddress;
        }
    }
}