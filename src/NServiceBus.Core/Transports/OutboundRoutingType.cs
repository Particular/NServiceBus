namespace NServiceBus.Transport
{
    /// <summary>
    /// The type of routing from the perspective of a transport.
    /// </summary>
    public enum OutboundRoutingType
    {
        /// <summary>
        /// Unicast. Routing is performed by the core and one send operation might require multiple calls to
        /// <see cref="IDispatchMessages" />.
        /// </summary>
        Unicast,

        /// <summary>
        /// Multicast. Routing is performed by the transport.
        /// </summary>
        Multicast
    }
}