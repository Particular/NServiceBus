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
        RoutingComponent(UnicastRoutingTable unicastRoutingTable, DistributionPolicy distributionPolicy, EndpointInstances endpointInstances, Publishers publishers, bool enforceBestPractices)
        {
            UnicastRoutingTable = unicastRoutingTable;
            DistributionPolicy = distributionPolicy;
            EndpointInstances = endpointInstances;
            Publishers = publishers;
            EnforceBestPractices = enforceBestPractices;
        }

        public UnicastRoutingTable UnicastRoutingTable { get; }

        public DistributionPolicy DistributionPolicy { get; }

        public EndpointInstances EndpointInstances { get; }

        public Publishers Publishers { get; }

        public bool EnforceBestPractices { get; }

        public static RoutingComponent Initialize(Configuration configuration, TransportComponent transportComponent, PipelineSettings pipelineSettings, ReceiveConfiguration receiveConfiguration, Conventions conventions)
        {
            var distributionPolicy = configuration.DistributionPolicy;
            foreach (var distributionStrategy in configuration.DistributionStrategies)
            {
                distributionPolicy.SetDistributionStrategy(distributionStrategy);
            }

            var unicastRoutingTable = configuration.UnicastRoutingTable;
            configuration.ConfiguredUnicastRoutes?.Apply(unicastRoutingTable, conventions);

            var endpointInstances = configuration.EndpointInstances;
            pipelineSettings.Register("UnicastSendRouterConnector", b =>
            {
                var router = new UnicastSendRouter(receiveConfiguration == null, receiveConfiguration?.QueueNameBase, receiveConfiguration?.InstanceSpecificQueue, distributionPolicy, unicastRoutingTable, endpointInstances, i => transportComponent.ToTransportAddress(LogicalAddress.CreateRemoteAddress(i)));
                return new SendConnector(router);
            }, "Determines how the message being sent should be routed");

            pipelineSettings.Register("UnicastReplyRouterConnector", new ReplyConnector(), "Determines how replies should be routed");

            if (configuration.EnforceBestPractices)
            {
                EnableBestPracticeEnforcement(conventions, pipelineSettings);
            }

            return new RoutingComponent(
                unicastRoutingTable,
                distributionPolicy,
                endpointInstances,
                configuration.Publishers,
                configuration.EnforceBestPractices);
        }

        static void EnableBestPracticeEnforcement(Conventions conventions, PipelineSettings pipeline)
        {
            var validations = new Validations(conventions);

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

            readonly SettingsHolder settings;

            const string EnforceBestPracticesSettingsKey = "NServiceBus.Routing.EnforceBestPractices";
        }
    }
}