namespace NServiceBus.Routing
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// A routing strategy for multicast routing.
    /// </summary>
    public class MulticastRoutingStrategy : RoutingStrategy
    {
        /// <summary>
        /// Creates new routing strategy.
        /// </summary>
        public MulticastRoutingStrategy(Type messageType)
        {
            this.messageType = messageType;
        }

        /// <summary>
        /// Applies the routing strategy to the message.
        /// </summary>
        /// <param name="headers">Message headers.</param>
        public override AddressTag Apply(Dictionary<string, string> headers)
        {
            return new MulticastAddressTag(messageType);
        }

        Type messageType;
    }
}