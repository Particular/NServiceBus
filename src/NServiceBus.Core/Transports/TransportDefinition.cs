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
        public virtual bool HasNativePubSubSupport { get { return false; } }

        /// <summary>
        /// Indicates that the transport has a central store for subscriptions
        /// </summary>
        public virtual bool HasSupportForCentralizedPubSub { get { return false; } }
    }
}