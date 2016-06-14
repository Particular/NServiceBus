namespace NServiceBus
{
    using System.Collections.Generic;
    using Routing;

    /// <summary>
    /// Defines distribution strategies for endpoints.
    /// </summary>
    public class DistributionPolicy : IDistributionPolicy
    {
        /// <summary>
        /// Assigns a custom distribution strategy to a given endpoint.
        /// </summary>
        /// <param name="endpointName">Endpoint.</param>
        /// <param name="distributionStrategy">Distribution strategy.</param>
        public void SetDistributionStrategy(string endpointName, DistributionStrategy distributionStrategy)
        {
            configuredStrategies[endpointName] = distributionStrategy;
        }

        /// <summary>
        /// Gets the distribution strategy for a given endpoint.
        /// </summary>
        /// <param name="endpointName">Endpoint.</param>
        /// <returns>Distribution strategy.</returns>
        public DistributionStrategy GetDistributionStrategy(string endpointName)
        {
            DistributionStrategy configuredStrategy;
            return configuredStrategies.TryGetValue(endpointName, out configuredStrategy) ? configuredStrategy : defaultStrategy;
        }

        Dictionary<string, DistributionStrategy> configuredStrategies = new Dictionary<string, DistributionStrategy>();
        DistributionStrategy defaultStrategy = new SingleInstanceRoundRobinDistributionStrategy();
    }
}