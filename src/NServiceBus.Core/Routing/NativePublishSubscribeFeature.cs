namespace NServiceBus.Features
{
    using Transport;

    class NativePublishSubscribeFeature : Feature
    {
        public NativePublishSubscribeFeature()
        {
            EnableByDefault();
            DependsOn<RoutingFeature>();
            Prerequisite(c => c.Settings.Get<TransportInfrastructure>().OutboundRoutingPolicy.Publishes == OutboundRoutingType.Multicast, "The transport does not support native pub sub");
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var transportInfrastructure = context.Settings.Get<TransportInfrastructure>();
            var canReceive = !context.Settings.GetOrDefault<bool>("Endpoint.SendOnly");
            var routing = context.Settings.Get<IRoutingComponent>();

            context.Pipeline.Register(new MulticastPublishRouterBehavior(), "Determines how the published messages should be routed");

            if (canReceive)
            {
                var transportSubscriptionInfrastructure = transportInfrastructure.ConfigureSubscriptionInfrastructure();
                var subscriptionManager = transportSubscriptionInfrastructure.SubscriptionManagerFactory();

                routing.RegisterSubscriptionHandler(_ => subscriptionManager);
            }
        }
    }
}