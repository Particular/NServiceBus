namespace NServiceBus.Features
{
    using Config;
    using Routing;
    using Routing.MessageDrivenSubscriptions;
    using Transport;

    class RoutingFeature : Feature
    {
        public RoutingFeature()
        {
            EnableByDefault();
            Defaults(s =>
            {
                s.SetDefault<ConfiguredUnicastRoutes>(new ConfiguredUnicastRoutes());
                s.SetDefault(EnforceBestPracticesSettingsKey, true);
                s.Set<RoutingFeature>(this);
            });
        }

        // it's no longer possible to directly access UnicastRoutingTable/Publishers from the settings -> only via feature (with a dependency on routing)
        // requires the RoutingFeature to be public to get access to these properties which were accessible via settings before
        public UnicastRoutingTable RoutingTable { get; private set; }
        public EndpointInstances Instances { get; private set; }
        public DistributionPolicy DistributionPolicy { get; private set; }
        public Publishers Publishers { get; private set; }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            RoutingTable = new UnicastRoutingTable();
            Instances = new EndpointInstances();
            DistributionPolicy = context.Settings.GetOrDefault<DistributionPolicy>() ?? new DistributionPolicy();
            Publishers = new Publishers();

            var canReceive = !context.Settings.GetOrDefault<bool>("Endpoint.SendOnly");
            var transportInfrastructure = context.Settings.Get<TransportInfrastructure>();
            var conventions = context.Settings.Get<Conventions>();
            var unicastBusConfig = context.Settings.GetConfigSection<UnicastBusConfig>();

            var configuredUnicastRoutes = context.Settings.Get<ConfiguredUnicastRoutes>();

            if (context.Settings.Get<bool>(EnforceBestPracticesSettingsKey))
            {
                EnableBestPracticeEnforcement(context);
            }

            unicastBusConfig?.MessageEndpointMappings.Apply(Publishers, RoutingTable, transportInfrastructure.MakeCanonicalForm, conventions);
            configuredUnicastRoutes.Apply(RoutingTable, conventions);

            context.Pipeline.Register(b =>
            {
                var unicastSendRouter = new UnicastSendRouter(RoutingTable, Instances, i => transportInfrastructure.ToTransportAddress(LogicalAddress.CreateRemoteAddress(i)));
                return new UnicastSendRouterConnector(context.Settings.LocalAddress(), context.Settings.InstanceSpecificQueue(), unicastSendRouter, DistributionPolicy, i => transportInfrastructure.ToTransportAddress(LogicalAddress.CreateRemoteAddress(i)));
            }, "Determines how the message being sent should be routed");

            context.Pipeline.Register(new UnicastReplyRouterConnector(), "Determines how replies should be routed");

            if (canReceive)
            {
                var publicReturnAddress = context.Settings.GetOrDefault<string>("PublicReturnAddress");
                var distributorAddress = context.Settings.GetOrDefault<string>("LegacyDistributor.Address");
                context.Pipeline.Register(new ApplyReplyToAddressBehavior(context.Settings.LocalAddress(), context.Settings.InstanceSpecificQueue(), publicReturnAddress, distributorAddress), "Applies the public reply to address to outgoing messages");
            }
        }

        void EnableBestPracticeEnforcement(FeatureConfigurationContext context)
        {
            var validations = new Validations(context.Settings.Get<Conventions>());

            context.Pipeline.Register(
                "EnforceSendBestPractices",
                new EnforceSendBestPracticesBehavior(validations),
                "Enforces send messaging best practices");

            context.Pipeline.Register(
                "EnforceReplyBestPractices",
                new EnforceReplyBestPracticesBehavior(validations),
                "Enforces reply messaging best practices");

            context.Pipeline.Register(
                "EnforcePublishBestPractices",
                new EnforcePublishBestPracticesBehavior(validations),
                "Enforces publish messaging best practices");

            context.Pipeline.Register(
                "EnforceSubscribeBestPractices",
                new EnforceSubscribeBestPracticesBehavior(validations),
                "Enforces subscribe messaging best practices");

            context.Pipeline.Register(
                "EnforceUnsubscribeBestPractices",
                new EnforceUnsubscribeBestPracticesBehavior(validations),
                "Enforces unsubscribe messaging best practices");
        }

        public const string EnforceBestPracticesSettingsKey = "NServiceBus.Routing.EnforceBestPractices";
    }
}