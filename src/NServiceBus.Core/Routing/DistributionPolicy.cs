namespace NServiceBus
{
    using System.Collections.Concurrent;
    using Routing;

    /// <summary>
    /// Configures distribution strategies.
    /// </summary>
    public class DistributionPolicy : IDistributionPolicy
    {
        /// <summary>
        /// Sets the distribution strategy for a given endpoint.
        /// </summary>
        /// <param name="distributionStrategy">Distribution strategy to be used.</param>
        public void SetDistributionStrategy(DistributionStrategy distributionStrategy)
        {
            Guard.AgainstNull(nameof(distributionStrategy), distributionStrategy);

            configuredStrategies[distributionStrategy.Endpoint] = distributionStrategy;
        }

        DistributionStrategy IDistributionPolicy.GetDistributionStrategy(string endpointName)
        {
            return configuredStrategies.GetOrAdd(endpointName, key => new SingleInstanceRoundRobinDistributionStrategy(key));
        }

        ConcurrentDictionary<string, DistributionStrategy> configuredStrategies = new ConcurrentDictionary<string, DistributionStrategy>();
    }
}