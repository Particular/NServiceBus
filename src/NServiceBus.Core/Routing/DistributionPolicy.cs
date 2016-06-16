namespace NServiceBus
{
    using System.Collections.Concurrent;
    using Routing;

    /// <summary>
    /// Allows to configure distribution strategies.
    /// </summary>
    public class DistributionPolicy : IDistributionPolicy
    {
        /// <summary>
        /// Sets the distribution strategy for a given endpoint.
        /// </summary>
        /// <param name="endpointName">Endpoint name.</param>
        /// <param name="distributionStrategy">Distribution strategy to be used.</param>
        public void SetDistributionStrategy(string endpointName, DistributionStrategy distributionStrategy)
        {
            configuredStrategies[endpointName] = distributionStrategy;
        }

        DistributionStrategy IDistributionPolicy.GetDistributionStrategy(string endpointName) => configuredStrategies.GetOrAdd(endpointName, key => new SingleInstanceRoundRobinDistributionStrategy());

        ConcurrentDictionary<string, DistributionStrategy> configuredStrategies = new ConcurrentDictionary<string, DistributionStrategy>();
    }
}