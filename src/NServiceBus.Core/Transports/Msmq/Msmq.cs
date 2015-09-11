namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using NServiceBus.ConsistencyGuarantees;
    using NServiceBus.Features;
    using NServiceBus.Performance.TimeToBeReceived;
    using NServiceBus.Support;
    using NServiceBus.Transports;

    /// <summary>
    /// Transport definition for MSMQ.
    /// </summary>
    public class MsmqTransport : TransportDefinition
    {
        /// <summary>
        /// Initializes a new instance of <see cref="MsmqTransport"/>.
        /// </summary>
        public MsmqTransport()
        {
            RequireOutboxConsent = true;
            HasSupportForMultiQueueNativeTransactions = true;
        }

        /// <summary>
        /// Gives implementations access to the <see cref="BusConfiguration"/> instance at configuration time.
        /// </summary>
        protected internal override void Configure(BusConfiguration config)
        {
            config.EnableFeature<MsmqTransportConfigurator>();
        }

        /// <summary>
        /// The list of constraints supported by the MSMQ transport.
        /// </summary>
        public override IEnumerable<Type> GetSupportedDeliveryConstraints()
        {
            return new[]
            {
                typeof(DiscardIfNotReceivedBefore)
            };
        }

        /// <summary>
        /// MSMQ will by default be exactly once using the DTC. 
        /// </summary>
        public override ConsistencyGuarantee GetDefaultConsistencyGuarantee()
        {
            return new ExactlyOnce();
        }

        /// <summary>
        /// Not used by the msmq transport.
        /// </summary>
        public override IManageSubscriptions GetSubscriptionManager()
        {
            throw new NotSupportedException("Msmq don't support native pub sub");
        }

        /// <summary>
        /// Returns the discriminator for this endpoint instance.
        /// </summary>
        public override string GetDiscriminatorForThisEndpointInstance()
        {
            return RuntimeEnvironment.MachineName;
        }

        /// <summary>
        /// Converts a given logical address to the transport address.
        /// </summary>
        /// <param name="logicalAddress">The logical address.</param>
        /// <returns>The transport address.</returns>
        public override string ToTransportAddress(LogicalAddress logicalAddress)
        {
            var machine = logicalAddress.EndpointInstanceName.TransportDiscriminator;

            var queue = new StringBuilder(logicalAddress.EndpointInstanceName.EndpointName.ToString());
            if (logicalAddress.EndpointInstanceName.UserDiscriminator != null)
            {
                queue.Append("-" + logicalAddress.EndpointInstanceName.UserDiscriminator);
            }
            if (logicalAddress.Qualifier != null)
            {
                queue.Append("." + logicalAddress.Qualifier);
            }
            return queue + "@" + machine;
        }
    }
}