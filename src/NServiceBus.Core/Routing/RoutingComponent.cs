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

        public bool EnforceBestPractices { get; private set; }

        public static RoutingComponent Initialize(SettingsHolder settings, TransportComponent transportComponent, PipelineSettings pipelineSettings, ReceiveConfiguration receiveConfiguration)
        {
            var conventions = settings.Get<Conventions>();
            var configuredUnicastRoutes = settings.GetOrDefault<ConfiguredUnicastRoutes>();

            var distributionPolicy = settings.GetOrCreate<DistributionPolicy>();
            if (settings.TryGet(out List<DistributionStrategy> distributionStrategies))
            {
                foreach (var distributionStrategy in distributionStrategies)
                {
                    distributionPolicy.SetDistributionStrategy(distributionStrategy);
                }
            }

            var unicastRoutingTable = settings.GetOrCreate<UnicastRoutingTable>();
            configuredUnicastRoutes?.Apply(unicastRoutingTable, conventions);

            var endpointInstances = settings.GetOrCreate<EndpointInstances>();
            pipelineSettings.Register("UnicastSendRouterConnector", b =>
            {
                var router = new UnicastSendRouter(receiveConfiguration == null, receiveConfiguration?.QueueNameBase, receiveConfiguration?.InstanceSpecificQueue, distributionPolicy, unicastRoutingTable, endpointInstances, i => transportComponent.ToTransportAddress(LogicalAddress.CreateRemoteAddress(i)));
                return new SendConnector(router);
            }, "Determines how the message being sent should be routed");

            pipelineSettings.Register("UnicastReplyRouterConnector", new ReplyConnector(), "Determines how replies should be routed");

            var enforceBestPractices = ShouldEnforceBestPractices(settings);
            if (enforceBestPractices)
            {
                EnableBestPracticeEnforcement(conventions, pipelineSettings);
            }

            var publishers = settings.GetOrCreate<Publishers>();
            return new RoutingComponent(unicastRoutingTable, distributionPolicy, endpointInstances, publishers, enforceBestPractices);
        }

        static bool ShouldEnforceBestPractices(ReadOnlySettings settings)
        {
            if (settings.TryGet(EnforceBestPracticesSettingsKey, out bool enforceBestPractices))
            {
                return enforceBestPractices;
            }

            // enable best practice enforcement by default
            return true;
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

        public const string EnforceBestPracticesSettingsKey = "NServiceBus.Routing.EnforceBestPractices";
    }
}