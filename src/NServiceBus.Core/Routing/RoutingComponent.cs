namespace NServiceBus.Features
{
    using Config;
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
        /// .
        /// </summary>
        /// <param name="unicastRoutingTable"></param>
        /// <param name="distributionPolicy"></param>
        /// <param name="endpointInstances"></param>
        /// <param name="publishers"></param>
        public RoutingComponent(UnicastRoutingTable unicastRoutingTable, DistributionPolicy distributionPolicy, EndpointInstances endpointInstances, Publishers publishers)
        {
            UnicastRoutingTable = unicastRoutingTable;
            DistributionPolicy = distributionPolicy;
            EndpointInstances = endpointInstances;
            Publishers = publishers;
        }

        /// <summary>
        /// Contains routing data for unicast send operations.
        /// </summary>
        public UnicastRoutingTable UnicastRoutingTable { get; }

        /// <summary>
        /// Provides distribution strategies for sender-side distribution.
        /// </summary>
        public DistributionPolicy DistributionPolicy { get; }

        /// <summary>
        /// Contains logical endpoint to physical instances mapping.
        /// </summary>
        public EndpointInstances EndpointInstances { get; }

        /// <summary>
        /// Contains routing data for subscription messages.
        /// </summary>
        public Publishers Publishers { get; }

        internal bool EnforceBestPractices { get; private set; }

        internal void Initialize(ReadOnlySettings settings, TransportInfrastructure transportInfrastructure, PipelineSettings pipelineSettings)
        {
            var unicastBusConfig = settings.GetConfigSection<UnicastBusConfig>();
            var conventions = settings.Get<Conventions>();
            var configuredUnicastRoutes = settings.GetOrDefault<ConfiguredUnicastRoutes>();

            unicastBusConfig?.MessageEndpointMappings.Apply(Publishers, UnicastRoutingTable, transportInfrastructure.MakeCanonicalForm, conventions);
            configuredUnicastRoutes?.Apply(UnicastRoutingTable, conventions);

            pipelineSettings.Register(b =>
            {
                var unicastSendRouter = new UnicastSendRouter(UnicastRoutingTable, EndpointInstances, i => transportInfrastructure.ToTransportAddress(LogicalAddress.CreateRemoteAddress(i)));
                return new UnicastSendRouterConnector(settings.LocalAddress(), settings.InstanceSpecificQueue(), unicastSendRouter, DistributionPolicy, i => transportInfrastructure.ToTransportAddress(LogicalAddress.CreateRemoteAddress(i)));
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