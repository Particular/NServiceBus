namespace NServiceBus.Transports
{
    /// <summary>
    /// Defines a transport that can be used by NServiceBus
    /// </summary>
    public abstract class TransportDefinition
    {
        /// <summary>
        /// Indicates that the transport is capable of supporting the publish and subscribe pattern natively
        /// </summary>
        public bool HasNativePubSubSupport { get; protected set; }

        /// <summary>
        /// Indicates that the transport has a central store for subscriptions
        /// </summary>
        public bool HasSupportForCentralizedPubSub { get; protected set; }

        /// <summary>
        /// Indicates that the transport has support for distributed transactions
        /// </summary>
        public bool? HasSupportForDistributedTransactions { get; protected set; }

        /// <summary>
        /// True if the transport
        /// </summary>
        public bool RequireOutboxConsent { get; set; }

        /// <summary>
        /// Gives implementations access to the <see cref="BusConfiguration"/> instance at configuration time.
        /// </summary>
        protected internal virtual void Configure(BusConfiguration config)
        {
            
        }
    }
}