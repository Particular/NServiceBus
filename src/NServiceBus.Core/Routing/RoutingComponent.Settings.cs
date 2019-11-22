namespace NServiceBus
{
    using System.Collections.Generic;
    using Features;
    using Routing;
    using Routing.MessageDrivenSubscriptions;
    using Settings;

    partial class RoutingComponent
    {
        public class Settings
        {
            public Settings(SettingsHolder settings)
            {
                this.settings = settings;
                EnforceBestPractices = true;
            }

            public ConfiguredUnicastRoutes ConfiguredUnicastRoutes => settings.GetOrCreate<ConfiguredUnicastRoutes>();

            // Used by NServiceBus.Transport.Msmq/MsmqConfigurationExtensions.cs
            public List<DistributionStrategy> DistributionStrategies => settings.GetOrDefault<List<DistributionStrategy>>();

            public UnicastRoutingTable UnicastRoutingTable => settings.GetOrCreate<UnicastRoutingTable>();

            public DistributionPolicy DistributionPolicy => settings.GetOrCreate<DistributionPolicy>();

            public EndpointInstances EndpointInstances => settings.GetOrCreate<EndpointInstances>();

            public Publishers Publishers => settings.GetOrCreate<Publishers>();

            public bool EnforceBestPractices
            {
                get => settings.Get<bool>("NServiceBus.Routing.EnforceBestPractices");
                set => settings.Set("NServiceBus.Routing.EnforceBestPractices", value);
            }

            public string PublicReturnAddress
            {
                get => settings.GetOrDefault<string>("PublicReturnAddress");
                set => settings.Set("PublicReturnAddress", value);
            }

            readonly SettingsHolder settings;
        }
    }
}