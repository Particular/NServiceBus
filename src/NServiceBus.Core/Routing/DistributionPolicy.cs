namespace NServiceBus
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using Routing;
    using Settings;

    /// <summary>
    /// Allows to configure distribution strategies.
    /// </summary>
    public class DistributionPolicy : IDistributionPolicy
    {
        ReadOnlySettings settings;

        /// <summary>
        /// Creates new instance of the policy.
        /// </summary>
        public DistributionPolicy(ReadOnlySettings settings)
        {
            this.settings = settings;
        }

        /// <summary>
        /// Sets the distribution strategy for a given endpoint.
        /// </summary>
        /// <param name="endpoint">Endpoint for which configure the strategy.</param>
        /// <param name="distributionStrategyFactory">Distribution strategy to be used.</param>
        public void SetDistributionStrategy(string endpoint, Func<ReadOnlySettings, DistributionStrategy> distributionStrategyFactory)
        {
            Guard.AgainstNull(nameof(distributionStrategyFactory), distributionStrategyFactory);

            configuredStrategyFactories[endpoint] = distributionStrategyFactory;
        }

        DistributionStrategy IDistributionPolicy.GetDistributionStrategy(string endpointName, DistributionStrategyScope scope)
        {
            return configuredStrategies.GetOrAdd(Tuple.Create(endpointName, scope), k => CreateStrategy(k.Item1));
        }

        DistributionStrategy CreateStrategy(string key)
        {
            Func<ReadOnlySettings, DistributionStrategy> factory;
            if (configuredStrategyFactories.TryGetValue(key, out factory))
            {
                return factory(settings);
            }
            return new SingleInstanceRoundRobinDistributionStrategy();
        }

        Dictionary<string, Func<ReadOnlySettings, DistributionStrategy>> configuredStrategyFactories = new Dictionary<string, Func<ReadOnlySettings, DistributionStrategy>>();
        ConcurrentDictionary<Tuple<string, DistributionStrategyScope>, DistributionStrategy> configuredStrategies = new ConcurrentDictionary<Tuple<string, DistributionStrategyScope>, DistributionStrategy>();
    }
}