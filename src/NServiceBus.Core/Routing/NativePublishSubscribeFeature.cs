namespace NServiceBus.Features
{
    using Transport;

    class NativePublishSubscribeFeature : Feature
    {
        public NativePublishSubscribeFeature()
        {
            EnableByDefault();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var transportInfrastructure = context.Settings.Get<TransportInfrastructure>();
            var canReceive = !context.Settings.GetOrDefault<bool>("Endpoint.SendOnly");

            context.Pipeline.Register("MulticastPublishRouterBehavior", new MulticastPublishConnector(), "Determines how the published messages should be routed");

            if (canReceive)
            {
                var transportSubscriptionInfrastructure = transportInfrastructure.ConfigureSubscriptionInfrastructure();
                var subscriptionManager = transportSubscriptionInfrastructure.SubscriptionManagerFactory();

                context.Pipeline.Register(new NativeSubscribeTerminator(subscriptionManager), "Requests the transport to subscribe to a given message type");
                context.Pipeline.Register(new NativeUnsubscribeTerminator(subscriptionManager), "Requests the transport to unsubscribe to a given message type");
            }
            else
            {
                context.Pipeline.Register(new SendOnlySubscribeTerminator(), "Throws an exception when trying to subscribe from a send-only endpoint");
                context.Pipeline.Register(new SendOnlyUnsubscribeTerminator(), "Throws an exception when trying to unsubscribe from a send-only endpoint");
            }
        }
    }
}