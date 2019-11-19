namespace NServiceBus
{
    using System.Collections.Generic;
    using Features;
    using Pipeline;
    using Routing;
    using Routing.MessageDrivenSubscriptions;
    using Settings;

    class RoutingComponent
    {
        RoutingComponent(UnicastRoutingTable unicastRoutingTable,
            DistributionPolicy distributionPolicy,
            EndpointInstances endpointInstances,
            Publishers publishers,
            UnicastSendRouter unicastSendRouter,
            bool enforceBestPractices,
            Validations messageValidator)
        {
            UnicastRoutingTable = unicastRoutingTable;
            DistributionPolicy = distributionPolicy;
            EndpointInstances = endpointInstances;
            Publishers = publishers;
            EnforceBestPractices = enforceBestPractices;
            MessageValidator = messageValidator;
            UnicastSendRouter = unicastSendRouter;
        }

        public UnicastRoutingTable UnicastRoutingTable { get; }

        public DistributionPolicy DistributionPolicy { get; }

        public EndpointInstances EndpointInstances { get; }

        public Publishers Publishers { get; }

        public bool EnforceBestPractices { get; }

        public UnicastSendRouter UnicastSendRouter { get; }

        public Validations MessageValidator { get; }

        public static RoutingComponent Initialize(Configuration configuration,
            TransportComponent.Configuration transportConfiguration,
            ReceiveComponent.ReceiveConfiguration receiveConfiguration,
            Conventions conventions,
            PipelineSettings pipelineSettings)
        {
            var distributionPolicy = configuration.DistributionPolicy;
            var unicastRoutingTable = configuration.UnicastRoutingTable;
            var endpointInstances = configuration.EndpointInstances;

            foreach (var distributionStrategy in configuration.DistributionStrategies)
            {
                distributionPolicy.SetDistributionStrategy(distributionStrategy);
            }

            configuration.ConfiguredUnicastRoutes?.Apply(unicastRoutingTable, conventions);

            var isSendOnlyEndpoint = receiveConfiguration == null;
            if (isSendOnlyEndpoint == false)
            {
                pipelineSettings.Register(new ApplyReplyToAddressBehavior(
                        receiveConfiguration.LocalAddress,
                        receiveConfiguration.InstanceSpecificQueue,
                        configuration.PublicReturnAddress),
                    "Applies the public reply to address to outgoing messages");
            }

            var sendRouter = new UnicastSendRouter(
                isSendOnlyEndpoint,
                receiveConfiguration?.QueueNameBase,
                receiveConfiguration?.InstanceSpecificQueue,
                distributionPolicy,
                unicastRoutingTable,
                endpointInstances,
                i => transportConfiguration.ToTransportAddress(LogicalAddress.CreateRemoteAddress(i)));

            return new RoutingComponent(
                unicastRoutingTable,
                distributionPolicy,
                endpointInstances,
                configuration.Publishers,
                sendRouter,
                configuration.EnforceBestPractices,
                new Validations(conventions));
        }

        public class Configuration
        {
            public Configuration(SettingsHolder settings)
            {
                this.settings = settings;
            }

            public ConfiguredUnicastRoutes ConfiguredUnicastRoutes => settings.GetOrCreate<ConfiguredUnicastRoutes>();

            // Used by NServiceBus.Transport.Msmq/MsmqConfigurationExtensions.cs
            public List<DistributionStrategy> DistributionStrategies =>
                settings.GetOrDefault<List<DistributionStrategy>>() ?? new List<DistributionStrategy>(0);

            public UnicastRoutingTable UnicastRoutingTable => settings.GetOrCreate<UnicastRoutingTable>();

            public DistributionPolicy DistributionPolicy => settings.GetOrCreate<DistributionPolicy>();

            public EndpointInstances EndpointInstances => settings.GetOrCreate<EndpointInstances>();

            public Publishers Publishers => settings.GetOrCreate<Publishers>();

            public bool EnforceBestPractices
            {
                get
                {
                    if (settings.TryGet(EnforceBestPracticesSettingsKey, out bool enforceBestPractices))
                    {
                        return enforceBestPractices;
                    }

                    // enable best practice enforcement by default
                    return true;
                }
                set => settings.Set(EnforceBestPracticesSettingsKey, value);
            }

            public string PublicReturnAddress => settings.GetOrDefault<string>(PublicReturnAddressSettingsKey);

            readonly SettingsHolder settings;

            const string EnforceBestPracticesSettingsKey = "NServiceBus.Routing.EnforceBestPractices";
            const string PublicReturnAddressSettingsKey = "PublicReturnAddress";
        }
    }
}