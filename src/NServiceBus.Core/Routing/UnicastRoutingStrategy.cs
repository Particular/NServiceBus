namespace NServiceBus.Routing
{
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
        public override AddressTag Apply(HeaderDictionary headers)
        {
            return new UnicastAddressTag(destination);
        }

        string destination;
    }
}
