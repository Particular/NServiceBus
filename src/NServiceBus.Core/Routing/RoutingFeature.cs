namespace NServiceBus.Features
{
    using Config;
    using Routing;
    using Routing.MessageDrivenSubscriptions;
    using Transport;

    class RoutingFeature : Feature
    {
        public const string EnforceBestPracticesSettingsKey = "NServiceBus.Routing.EnforceBestPractices";

        public RoutingFeature()
        {
            EnableByDefault();
            Defaults(s =>
            {
                s.SetDefault<UnicastRoutingTable>(new UnicastRoutingTable());
                s.SetDefault<ConfiguredUnicastRoutes>(new ConfiguredUnicastRoutes());

                s.SetDefault<EndpointInstances>(new EndpointInstances());
                s.SetDefault<DistributionPolicy>(new DistributionPolicy());

                s.SetDefault(EnforceBestPracticesSettingsKey, true);

                s.SetDefault<Publishers>(new Publishers()); // required to initialize MessageEndpointMappings.
            });
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var canReceive = !context.Settings.GetOrDefault<bool>("Endpoint.SendOnly");
            var transportInfrastructure = context.Settings.Get<TransportInfrastructure>();
            var conventions = context.Settings.Get<Conventions>();
            var unicastBusConfig = context.Settings.GetConfigSection<UnicastBusConfig>();

            var unicastRoutingTable = context.Settings.Get<UnicastRoutingTable>();
            var endpointInstances = context.Settings.Get<EndpointInstances>();
            var publishers = context.Settings.Get<Publishers>();
            var distributionPolicy = context.Settings.Get<DistributionPolicy>();
            var configuredUnicastRoutes = context.Settings.Get<ConfiguredUnicastRoutes>();
            var distributorAddress = context.Settings.GetOrDefault<string>("LegacyDistributor.Address");

            if (context.Settings.Get<bool>(EnforceBestPracticesSettingsKey))
            {
                EnableBestPracticeEnforcement(context);
            }

            unicastBusConfig?.MessageEndpointMappings.Apply(publishers, unicastRoutingTable, transportInfrastructure.MakeCanonicalForm, conventions);
            configuredUnicastRoutes.Apply(unicastRoutingTable, conventions);

            context.Pipeline.Register(b =>
            {
                var unicastSendRouter = new UnicastSendRouter(unicastRoutingTable, endpointInstances, i => transportInfrastructure.ToTransportAddress(LogicalAddress.CreateRemoteAddress(i)));
                return new UnicastSendRouterConnector(context.Settings.LocalAddress(), context.Settings.InstanceSpecificQueue(), distributorAddress, unicastSendRouter, distributionPolicy, i => transportInfrastructure.ToTransportAddress(LogicalAddress.CreateRemoteAddress(i)));
            }, "Determines how the message being sent should be routed");

            context.Pipeline.Register(new UnicastReplyRouterConnector(), "Determines how replies should be routed");

            if (canReceive)
            {
                var publicReturnAddress = context.Settings.GetOrDefault<string>("PublicReturnAddress");
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
    }
}