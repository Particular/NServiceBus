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
        RoutingComponent(UnicastSendRouter unicastSendRouter, bool enforceBestPractices)
        {
            EnforceBestPractices = enforceBestPractices;
            UnicastSendRouter = unicastSendRouter;
        }

        public bool EnforceBestPractices { get; }

        public UnicastSendRouter UnicastSendRouter { get; }

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

        public static RoutingComponent Initialize(
            Configuration configuration,
            TransportSeam transportSeam,
            ReceiveComponent.Configuration receiveConfiguration,
            Conventions conventions,
            PipelineSettings pipelineSettings)
        {
            var distributionPolicy = configuration.DistributionPolicy;
            var unicastRoutingTable = configuration.UnicastRoutingTable;
            var endpointInstances = configuration.EndpointInstances;

            foreach (var distributionStrategy in configuration.CustomDistributionStrategies)
            {
                distributionPolicy.SetDistributionStrategy(distributionStrategy);
            }

            configuration.ConfiguredUnicastRoutes?.Apply(unicastRoutingTable, conventions);

            var isSendOnlyEndpoint = receiveConfiguration.IsSendOnlyEndpoint;
            if (!isSendOnlyEndpoint)
            {
                pipelineSettings.Register(
                    new ApplyReplyToAddressBehavior(
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
                i => transportSeam.TransportInfrastructure.ToTransportAddress(LogicalAddress.CreateRemoteAddress(i)));

            if (configuration.EnforceBestPractices)
            {
                EnableBestPracticeEnforcement(pipelineSettings, new Validations(conventions));
            }

            return new RoutingComponent(
                sendRouter,
                configuration.EnforceBestPractices);
        }

        static void EnableBestPracticeEnforcement(PipelineSettings pipeline, Validations validations)
        {
            pipeline.Register(
                "EnforceSendBestPractices",
                new EnforceSendBestPracticesBehavior(validations),
                "Enforces send messaging best practices");

            pipeline.Register(
                "EnforceReplyBestPractices",
                new EnforceReplyBestPracticesBehavior(validations),
                "Enforces reply messaging best practices");

            pipeline.Register(
                "EnforcePublishBestPractices",
                new EnforcePublishBestPracticesBehavior(validations),
                "Enforces publish messaging best practices");

            pipeline.Register(
                "EnforceSubscribeBestPractices",
                new EnforceSubscribeBestPracticesBehavior(validations),
                "Enforces subscribe messaging best practices");

            pipeline.Register(
                "EnforceUnsubscribeBestPractices",
                new EnforceUnsubscribeBestPracticesBehavior(validations),
                "Enforces unsubscribe messaging best practices");
        }

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

        public class Configuration
        {
            public Configuration(UnicastRoutingTable unicastRoutingTable,
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