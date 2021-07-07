namespace NServiceBus.Routing
{
    /// <summary>
    /// An abstraction that defines how a message is going to be routed.
    /// </summary>
    public abstract class RoutingStrategy
    {
        /// <summary>
        /// Applies the routing strategy to the message.
        /// </summary>
        /// <param name="headers">Message headers.</param>
        public abstract AddressTag Apply(HeaderDictionary headers);
    }
}