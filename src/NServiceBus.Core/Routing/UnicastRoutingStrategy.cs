namespace NServiceBus.Routing
{
    using System.Collections.Generic;

    /// <summary>
    /// A routing strategy for unicast routing.
    /// </summary>
    public class UnicastRoutingStrategy : RoutingStrategy
    {
        /// <summary>
        /// Creates new routing strategy.
        /// </summary>
        public UnicastRoutingStrategy(string destination)
        {
            this.destination = destination;
        }

        /// <summary>
        /// Applies the routing strategy to the message.
        /// </summary>
        /// <param name="headers">Message headers.</param>
        public override AddressTag Apply(Dictionary<string, string> headers)
        {
            return new UnicastAddressTag(destination);
        }

        string destination;
    }
}