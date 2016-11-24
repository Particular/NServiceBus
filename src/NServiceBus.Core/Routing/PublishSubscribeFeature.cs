namespace NServiceBus
{
    using Features;

    class PublishSubscribeFeature : Feature
    {
        public PublishSubscribeFeature()
        {
            EnableByDefault();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var canReceive = !context.Settings.GetOrDefault<bool>("Endpoint.SendOnly");

            var publishSubscribeProvider = context.Settings.Get<IPublishSubscribeProvider>();

            var routerFactory = publishSubscribeProvider.GetRouter(context);
            context.Container.ConfigureComponent(routerFactory, DependencyLifecycle.SingleInstance);

            context.Pipeline.Register(typeof(PublishRouterBehavior), "Determines how the published messages should be routed");

            if (canReceive)
            {
                var subscriptionManagerFactory = publishSubscribeProvider.GetSubscriptionManager(context);
                context.Container.ConfigureComponent(subscriptionManagerFactory, DependencyLifecycle.SingleInstance);

                context.Pipeline.Register(typeof(NativeSubscribeTerminator), "Requests the transport to subscribe to a given message type");
                context.Pipeline.Register(typeof(NativeUnsubscribeTerminator), "Requests the transport to unsubscribe to a given message type");
            }
        }
    }
}