namespace NServiceBus
{
    using System.Collections.Generic;
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
            return configuredStrategies.TryGetValue(endpointName, out configuredStrategy) ? configuredStrategy : defaultStrategy;
        }

        Dictionary<string, DistributionStrategy> configuredStrategies = new Dictionary<string, DistributionStrategy>();
        DistributionStrategy defaultStrategy = new SingleInstanceRoundRobinDistributionStrategy();
    }
}