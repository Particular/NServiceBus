namespace NServiceBus.Transport
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Routing;

    /// <summary>
    /// Transport infrastructure definitions.
    /// </summary>
    public abstract class TransportInfrastructure
    {
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
        /// True if the transport.
        /// </summary>
        public bool RequireOutboxConsent { get; protected set; }

        /// <summary>
        /// Gets the factories to receive message.
        /// </summary>
        public abstract TransportReceiveInfrastructure ConfigureReceiveInfrastructure();

        /// <summary>
        /// Gets the factories to send message.
        /// </summary>
        public abstract TransportSendInfrastructure ConfigureSendInfrastructure();

        /// <summary>
        /// Gets the factory to manage subscriptions.
        /// </summary>
        public abstract TransportSubscriptionInfrastructure ConfigureSubscriptionInfrastructure();

        /// <summary>
        /// Returns the discriminator for this endpoint instance.
        /// </summary>
        public abstract EndpointInstance BindToLocalEndpoint(EndpointInstance instance);

        /// <summary>
        /// Converts a given logical address to the transport address.
        /// </summary>
        /// <param name="logicalAddress">The logical address.</param>
        /// <returns>The transport address.</returns>
        public abstract string ToTransportAddress(LogicalAddress logicalAddress);

        /// <summary>
        /// Returns the canonical for of the given transport address so various transport addresses can be effectively compared and
        /// de-duplicated.
        /// </summary>
        /// <param name="transportAddress">A transport address.</param>
        public virtual string MakeCanonicalForm(string transportAddress)
        {
            return transportAddress;
        }

        /// <summary>
        /// Performs any action required to warm up the transport infrastructure before starting the endpoint.
        /// </summary>
        public virtual Task Start()
        {
            return TaskEx.CompletedTask;
        }

        /// <summary>
        /// Performs any action required to cool down the transport infrastructure when the endpoint is stopping.
        /// </summary>
        public virtual Task Stop()
        {
            return TaskEx.CompletedTask;
        }
    }
}