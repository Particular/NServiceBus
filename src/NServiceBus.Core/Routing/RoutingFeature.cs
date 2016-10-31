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
                s.SetDefault<UnicastRoutingTable>(new UnicastRoutingTable());
                s.SetDefault<ConfiguredUnicastRoutes>(new ConfiguredUnicastRoutes());

                s.SetDefault<EndpointInstances>(new EndpointInstances());
                s.SetDefault<DistributionPolicy>(new DistributionPolicy());

                s.SetDefault<Publishers>(new Publishers()); // required to initialize MessageEndpointMappings. Also initialized in PublishSubscribeFeature.
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

            unicastBusConfig?.MessageEndpointMappings.Apply(publishers, unicastRoutingTable, transportInfrastructure.MakeCanonicalForm, conventions);
            configuredUnicastRoutes.Apply(unicastRoutingTable, conventions);

            context.Pipeline.Register(b =>
            {
                var unicastSendRouter = new UnicastSendRouter(unicastRoutingTable, endpointInstances, i => transportInfrastructure.ToTransportAddress(LogicalAddress.CreateRemoteAddress(i)));
                return new UnicastSendRouterConnector(context.Settings.LocalAddress(), context.Settings.InstanceSpecificQueue(), unicastSendRouter, distributionPolicy, i => transportInfrastructure.ToTransportAddress(LogicalAddress.CreateRemoteAddress(i)));
            }, "Determines how the message being sent should be routed");

            context.Pipeline.Register(new UnicastReplyRouterConnector(), "Determines how replies should be routed");

            if (canReceive)
            {
                var publicReturnAddress = context.Settings.GetOrDefault<string>("PublicReturnAddress");
                var distributorAddress = context.Settings.GetOrDefault<string>("LegacyDistributor.Address");
                context.Pipeline.Register(new ApplyReplyToAddressBehavior(context.Settings.LocalAddress(), context.Settings.InstanceSpecificQueue(), publicReturnAddress, distributorAddress), "Applies the public reply to address to outgoing messages");
            }
        }
    }
}