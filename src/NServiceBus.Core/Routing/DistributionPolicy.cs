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
            //Following approch trades off absolute correctness for performance.
            //There might be cases where two different strategies will be returned for a given endpoint but this is fine.
            DistributionStrategy configuredStrategy;
            if (configuredStrategies.TryGetValue(endpointName, out configuredStrategy))
            {
                return configuredStrategy;
            }
            var newConfiguredStrategies = new Dictionary<string, DistributionStrategy>(configuredStrategies);
            var defaultStrategy = new SingleInstanceRoundRobinDistributionStrategy();
            newConfiguredStrategies[endpointName] = defaultStrategy;
            configuredStrategies = newConfiguredStrategies;
            return defaultStrategy;
        }

        Dictionary<string, DistributionStrategy> configuredStrategies = new Dictionary<string, DistributionStrategy>();
    }
}