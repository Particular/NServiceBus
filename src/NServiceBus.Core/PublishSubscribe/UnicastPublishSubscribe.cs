namespace NServiceBus.Features
{
    class UnicastPublishSubscribe : Feature
    {
        public UnicastPublishSubscribe()
        {
            EnableByDefault();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var canReceive = !context.Settings.GetOrDefault<bool>("Endpoint.SendOnly");

            context.Pipeline.Register(b => new UnicastPublishRouterConnector(b.Build<IUnicastPublishSubscribe>()), "Determines how the published messages should be routed");

            if (canReceive)
            {
                context.Pipeline.Register(b => new NonNativePublishSubscribeTerminator(b.Build<IUnicastPublishSubscribe>()), "Handles subscribe requests for non-native publish subscribe.");
                context.Pipeline.Register(b => new NonNativePublishUnsubscribeTerminator(b.Build<IUnicastPublishSubscribe>()), "Handles unsubscribe requests for non-native publish subscribe.");
            }
        }
    }
}