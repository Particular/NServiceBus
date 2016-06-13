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

        public DistributionStrategy GetDistributionStrategy(string endpointName)
        {
            DistributionStrategy configuredStrategy;
            return configuredStrategies.TryGetValue(endpointName, out configuredStrategy) 
                ? configuredStrategy 
                : configuredStrategies.AddOrUpdate(endpointName, new SingleInstanceRoundRobinDistributionStrategy(), (e, strategy) => strategy);
        }

        ConcurrentDictionary<string, DistributionStrategy> configuredStrategies = new ConcurrentDictionary<string, DistributionStrategy>();
    }
}