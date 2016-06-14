namespace NServiceBus
{
    using System.Collections.Concurrent;
    using Routing;

    class DistributionPolicy : IDistributionPolicy
    {
        public void SetDistributionStrategy(string endpointName, DistributionStrategy distributionStrategy)
        {
            configuredStrategies[endpointName] = distributionStrategy;
        }

        public DistributionStrategy GetDistributionStrategy(string endpointName) => configuredStrategies.GetOrAdd(endpointName, key => new SingleInstanceRoundRobinDistributionStrategy());

        ConcurrentDictionary<string, DistributionStrategy> configuredStrategies = new ConcurrentDictionary<string, DistributionStrategy>();
    }
}