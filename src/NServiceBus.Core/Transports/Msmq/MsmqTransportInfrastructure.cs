namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using NServiceBus.Routing;
    using NServiceBus.Settings;
    using NServiceBus.Transports;

    /// <summary>
    /// Transport infrastructure for MSMQ.
    /// </summary>
    public class MsmqTransportInfrastructure : TransportInfrastructure
    {
        /// <summary>
        /// MsmqTransportInfrastructure.
        /// </summary>
        /// <param name="deliveryConstraints">The delivery constraints.</param>
        /// <param name="transactionMode">The transaction mode.</param>
        /// <param name="outboundRoutingPolicy">The outbound routing policy.</param>
        /// <param name="configureSendInfrastructure">The send infrastructure factory.</param>
        /// <param name="configureReceiveInfrastructure">The receive infrastructure factory.</param>
        /// <param name="configureSubscriptionInfrastructure">The subscription infrastructure factory.</param>
        public MsmqTransportInfrastructure(IEnumerable<Type> deliveryConstraints, TransportTransactionMode transactionMode, OutboundRoutingPolicy outboundRoutingPolicy, Func<string, TransportSendInfrastructure> configureSendInfrastructure, Func<string, TransportReceiveInfrastructure> configureReceiveInfrastructure = null, Func<TransportSubscriptionInfrastructure> configureSubscriptionInfrastructure = null) : base(deliveryConstraints, transactionMode, outboundRoutingPolicy, configureSendInfrastructure, configureReceiveInfrastructure, configureSubscriptionInfrastructure)
        {
            RequireOutboxConsent = true;
        }

        /// <summary>
        /// Returns the discriminator for this endpoint instance.
        /// </summary>
        public override EndpointInstance BindToLocalEndpoint(EndpointInstance instance, ReadOnlySettings settings) => instance.AtMachine(Environment.MachineName);

        /// <summary>
        /// Converts a given logical address to the transport address.
        /// </summary>
        /// <param name="logicalAddress">The logical address.</param>
        /// <returns>The transport address.</returns>
        public override string ToTransportAddress(LogicalAddress logicalAddress)
        {
            string machine;
            if (!logicalAddress.EndpointInstance.Properties.TryGetValue("Machine", out machine))
            {
                machine = Environment.MachineName;
            }

            var queue = new StringBuilder(logicalAddress.EndpointInstance.Endpoint.ToString());
            if (logicalAddress.EndpointInstance.Discriminator != null)
            {
                queue.Append("-" + logicalAddress.EndpointInstance.Discriminator);
            }
            if (logicalAddress.Qualifier != null)
            {
                queue.Append("." + logicalAddress.Qualifier);
            }
            return queue + "@" + machine;
        }

        /// <summary>
        /// Returns the canonical for of the given transport address so various transport addresses can be effectively compared and deduplicated.
        /// </summary>
        /// <param name="transportAddress">A transport address.</param>
        public override string MakeCanonicalForm(string transportAddress)
        {
            return MsmqAddress.Parse(transportAddress).ToString();
        }

        /// <summary>
        /// Gets an example connection string to use when reporting lack of configured connection string to the user.
        /// </summary>
        public override string ExampleConnectionStringForErrorMessage => "cacheSendConnection=true;journal=false;deadLetter=true";

        /// <summary>
        /// Used by implementations to control if a connection string is necessary.
        /// </summary>
        public override bool RequiresConnectionString => false;
    }
}