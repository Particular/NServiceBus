namespace NServiceBus.Features
{
    using Transport;

    class NativePublishSubscribeFeature : Feature
    {
        public NativePublishSubscribeFeature()
        {
            EnableByDefault();
            Prerequisite(c => c.Settings.Get<TransportInfrastructure>().OutboundRoutingPolicy.Publishes == OutboundRoutingType.Multicast, "The transport does not support native pub sub");
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var transportInfrastructure = context.Settings.Get<TransportInfrastructure>();
            var canReceive = !context.Settings.GetOrDefault<bool>("Endpoint.SendOnly");

            context.Pipeline.Register(new MulticastPublishRouterBehavior(), "Determines how the published messages should be routed");

            if (canReceive)
            {
                var transportSubscriptionInfrastructure = transportInfrastructure.ConfigureSubscriptionInfrastructure();
                var subscriptionManager = transportSubscriptionInfrastructure.SubscriptionManagerFactory();

                context.Pipeline.Register(new NativeSubscribeTerminator(subscriptionManager), "Requests the transport to subscribe to a given message type");
                context.Pipeline.Register(new NativeUnsubscribeTerminator(subscriptionManager), "Requests the transport to unsubscribe to a given message type");
            }
        }
    }
}