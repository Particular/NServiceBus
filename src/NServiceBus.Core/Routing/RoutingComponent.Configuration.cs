namespace NServiceBus
{
    using System.Collections.Generic;
    using Features;
    using Routing;
    using Routing.MessageDrivenSubscriptions;

    partial class RoutingComponent
    {
        public static Configuration Configure(Settings settings)
        {
            return new Configuration(
                settings.UnicastRoutingTable,
                settings.Publishers,
                settings.DistributionPolicy,
                settings.ConfiguredUnicastRoutes,
                settings.DistributionStrategies ?? new List<DistributionStrategy>(0),
                settings.EndpointInstances,
                settings.EnforceBestPractices,
                settings.PublicReturnAddress);
        }

        public class Configuration
        {
            public Configuration(
                UnicastRoutingTable unicastRoutingTable,
                Publishers publishers,
                DistributionPolicy distributionPolicy,
                ConfiguredUnicastRoutes configuredUnicastRoutes,
                IReadOnlyList<DistributionStrategy> customDistributionStrategies,
                EndpointInstances settingsEndpointInstances,
                bool enforceBestPractices,
                string returnAddressOverride)
            {
                UnicastRoutingTable = unicastRoutingTable;
                Publishers = publishers;
                DistributionPolicy = distributionPolicy;
                CustomDistributionStrategies = customDistributionStrategies;
                ConfiguredUnicastRoutes = configuredUnicastRoutes;
                EnforceBestPractices = enforceBestPractices;
                PublicReturnAddress = returnAddressOverride;
                EndpointInstances = settingsEndpointInstances;
            }

            public ConfiguredUnicastRoutes ConfiguredUnicastRoutes { get; }

            public IReadOnlyList<DistributionStrategy> CustomDistributionStrategies { get; }

            public UnicastRoutingTable UnicastRoutingTable { get; }

            public DistributionPolicy DistributionPolicy { get; }

            public EndpointInstances EndpointInstances { get; }

            public Publishers Publishers { get; }

            public bool EnforceBestPractices { get; }

            public string PublicReturnAddress { get; }
        }
    }
}