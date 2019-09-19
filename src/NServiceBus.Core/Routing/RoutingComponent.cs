namespace NServiceBus
{
    using System.Collections.Generic;
    using Features;
    using NServiceBus.Transport;
    using Pipeline;
    using Routing;
    using Routing.MessageDrivenSubscriptions;
    using Settings;

    class RoutingComponent
    {
        public const string EnforceBestPracticesSettingsKey = "NServiceBus.Routing.EnforceBestPractices";

        public RoutingComponent(SettingsHolder settings)
        {
            // use GetOrCreate to use of instances already created during EndpointConfiguration.
            UnicastRoutingTable = settings.GetOrCreate<UnicastRoutingTable>();
            DistributionPolicy = settings.GetOrCreate<DistributionPolicy>();
            EndpointInstances = settings.GetOrCreate<EndpointInstances>();
            Publishers = settings.GetOrCreate<Publishers>();
            this.settings = settings;
        }

        public UnicastRoutingTable UnicastRoutingTable { get; }

        public DistributionPolicy DistributionPolicy { get; }

        public EndpointInstances EndpointInstances { get; }

        public Publishers Publishers { get; }

        public bool EnforceBestPractices { get; private set; }

        public void Initialize(TransportInfrastructure transportInfrastructure, PipelineComponent pipelineComponent, ReceiveConfiguration receiveConfiguration)
        {
            var conventions = settings.Get<Conventions>();
            var configuredUnicastRoutes = settings.GetOrDefault<ConfiguredUnicastRoutes>();

            if (settings.TryGet(out List<DistributionStrategy> distributionStrategies))
            {
                foreach (var distributionStrategy in distributionStrategies)
                {
                    DistributionPolicy.SetDistributionStrategy(distributionStrategy);
                }
            }

            configuredUnicastRoutes?.Apply(UnicastRoutingTable, conventions);

            var pipelineSettings = pipelineComponent.PipelineSettings;

            pipelineSettings.Register(b =>
            {
                var router = new UnicastSendRouter(receiveConfiguration == null, receiveConfiguration?.QueueNameBase, receiveConfiguration?.InstanceSpecificQueue, DistributionPolicy, UnicastRoutingTable, EndpointInstances, i => transportInfrastructure.ToTransportAddress(LogicalAddress.CreateRemoteAddress(i)));
                return new UnicastSendRouterConnector(router);
            }, "Determines how the message being sent should be routed");

            pipelineSettings.Register(new UnicastReplyRouterConnector(), "Determines how replies should be routed");

            EnforceBestPractices = ShouldEnforceBestPractices(settings);
            if (EnforceBestPractices)
            {
                EnableBestPracticeEnforcement(conventions, pipelineSettings);
            }
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

        SettingsHolder settings;
    }
}