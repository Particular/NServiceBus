namespace NServiceBus.Routing
{
    using NServiceBus.Config;

    /// <summary>
    /// Base interface to implement to create different types of routing distributors.
    /// </summary>
    public interface IRouterDistributor
    {
        /// <summary>
        /// Returns the full address to send messages to based on the <paramref name="logicalEndpoint"/> provided.
        /// If no routing distribution is available for the <paramref name="logicalEndpoint"/>, the address specified in <see cref="UnicastBusConfig.MessageEndpointMappings"/> is used.
        /// </summary>
        /// <param name="logicalEndpoint">The name of the logical endpoint to get address fro</param>
        /// <param name="address">The full address to send to. (The address needs to be able to be parsable by <see cref="Address.Parse"/>)</param>
        /// <returns><code>true</code> if route exists, otherwise <code>false</code>.</returns>
        bool TryGetRouteAddress(string logicalEndpoint, out string address);
    }
}