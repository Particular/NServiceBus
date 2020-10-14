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
            ////var transportInfrastructure = context.Settings.Get<TransportInfrastructure>();

            ////context.Pipeline.Register("MulticastPublishRouterBehavior", new MulticastPublishConnector(), "Determines how the published messages should be routed");

            ////if (!context.Receiving.IsSendOnlyEndpoint)
            ////{
            ////    //// TODO we need some way to access the main receiver or at least it's subscription manager?
            ////    IManageSubscriptions subscriptions = null;

            ////    context.Pipeline.Register(new NativeSubscribeTerminator(subscriptions), "Requests the transport to subscribe to a given message type");
            ////    context.Pipeline.Register(new NativeUnsubscribeTerminator(subscriptions), "Requests the transport to unsubscribe to a given message type");
            ////}
            ////else
            ////{
            ////    context.Pipeline.Register(new SendOnlySubscribeTerminator(), "Throws an exception when trying to subscribe from a send-only endpoint");
            ////    context.Pipeline.Register(new SendOnlyUnsubscribeTerminator(), "Throws an exception when trying to unsubscribe from a send-only endpoint");
            ////}
        }
    }
}