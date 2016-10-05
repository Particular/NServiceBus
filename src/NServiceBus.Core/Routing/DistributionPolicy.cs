namespace NServiceBus
{
    using System;
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

            configuredStrategies[Tuple.Create(distributionStrategy.Endpoint, distributionStrategy.Scope)] = distributionStrategy;
        }

        DistributionStrategy IDistributionPolicy.GetDistributionStrategy(string endpointName, DistributionStrategyScope scope)
        {
            return configuredStrategies.GetOrAdd(Tuple.Create(endpointName, scope), key => new SingleInstanceRoundRobinDistributionStrategy(key.Item1, key.Item2));
        }

        ConcurrentDictionary<Tuple<string, DistributionStrategyScope>, DistributionStrategy> configuredStrategies = new ConcurrentDictionary<Tuple<string, DistributionStrategyScope>, DistributionStrategy>();
    }
}