namespace NServiceBus.Unicast.Routing
{
    using NServiceBus.Config;

    /// <summary>
    /// Base interface to implement to create different types of routing distributors.
    /// </summary>
    public interface IRouterDistributor
    {
        /// <summary>
        /// Returns the full address to send messages to based on the <paramref name="queueName"/> provided.
        /// If no routing distribution is available for the <paramref name="queueName"/>, the address specified in <see cref="UnicastBusConfig.MessageEndpointMappings"/> is used.
        /// </summary>
        /// <param name="queueName">The queue name the message is to be sent to.</param>
        /// <param name="address">The full address to send to. (The address needs to be able to be parsable by <see cref="Address.Parse"/>)</param>
        /// <returns><code>true</code> if route exists, otherwise <code>false</code>.</returns>
        bool TryGetRouteAddress(string queueName, out string address);
    }
}