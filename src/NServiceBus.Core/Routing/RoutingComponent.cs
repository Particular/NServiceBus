namespace NServiceBus
{
    using System.Collections.Generic;
    using Config;
    using Features;
    using Pipeline;
    using Routing;
    using Routing.MessageDrivenSubscriptions;
    using Settings;
    using Transport;

    /// <summary>
    /// Provides access to the core's routing infrastructure.
    /// </summary>
    public class RoutingComponent
    {
        internal const string EnforceBestPracticesSettingsKey = "NServiceBus.Routing.EnforceBestPractices";

        /// <summary>
        /// Contains routing data for unicast send operations.
        /// </summary>
        public UnicastRoutingTable UnicastRoutingTable { get; } = new UnicastRoutingTable();

        /// <summary>
        /// Provides distribution strategies for sender-side distribution.
        /// </summary>
        public DistributionPolicy DistributionPolicy { get; } = new DistributionPolicy();

        /// <summary>
        /// Contains logical endpoint to physical instances mapping.
        /// </summary>
        public EndpointInstances EndpointInstances { get; } = new EndpointInstances();

        /// <summary>
        /// Contains routing data for subscription messages.
        /// </summary>
        public Publishers Publishers { get; } = new Publishers();

        internal bool EnforceBestPractices { get; private set; }

        internal void Initialize(ReadOnlySettings settings, TransportInfrastructure transportInfrastructure, PipelineSettings pipelineSettings)
        {
            var unicastBusConfig = settings.GetConfigSection<UnicastBusConfig>();
            var conventions = settings.Get<Conventions>();
            var configuredUnicastRoutes = settings.GetOrDefault<ConfiguredUnicastRoutes>();
            var distributorAddress = settings.GetOrDefault<string>("LegacyDistributor.Address");

            List<DistributionStrategy> distributionStrategies;
            if (settings.TryGet(out distributionStrategies))
            {
                foreach (var distributionStrategy in distributionStrategies)
                {
                    DistributionPolicy.SetDistributionStrategy(distributionStrategy);
                }
            }

            unicastBusConfig?.MessageEndpointMappings.Apply(Publishers, UnicastRoutingTable, transportInfrastructure.MakeCanonicalForm, conventions);
            configuredUnicastRoutes?.Apply(UnicastRoutingTable, conventions);

            pipelineSettings.Register(b =>
            {
                var unicastSendRouter = new UnicastSendRouter(UnicastRoutingTable, EndpointInstances, i => transportInfrastructure.ToTransportAddress(LogicalAddress.CreateRemoteAddress(i)));
                return new UnicastSendRouterConnector(settings.LocalAddress(), settings.InstanceSpecificQueue(), distributorAddress, unicastSendRouter, DistributionPolicy, i => transportInfrastructure.ToTransportAddress(LogicalAddress.CreateRemoteAddress(i)));
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
            bool enforceBestPractices;
            if (settings.TryGet(EnforceBestPracticesSettingsKey, out enforceBestPractices))
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
    }
}