namespace NServiceBus.Features
{
    using System.Linq;
    using NServiceBus.Config;
    using NServiceBus.Routing.StorageDrivenPublishing;
    using NServiceBus.TransportDispatch;
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Messages;
    using NServiceBus.Unicast.Routing;
    using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;

    class RoutingFeature : Feature
    {
        public RoutingFeature()
        {
            EnableByDefault();
        }
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Pipeline.Register("DetermineRouteForSend", typeof(DetermineRouteForSendBehavior), "Determines how the message being sent should be routed");
            context.Pipeline.Register("DetermineRouteForReply", typeof(DetermineRouteForReplyBehavior), "Determines how replies should be routed");
            context.Pipeline.Register("DetermineRouteForPublish", typeof(DetermineRouteForPublishBehavior), "Determines how the published messages should be routed");

            if (!context.Settings.GetOrDefault<bool>("Endpoint.SendOnly"))
            {
                context.Pipeline.Register("ApplyReplyToAddress", typeof(ApplyReplyToAddressBehavior), "Applies the public reply to address to outgoing messages");

                string replyToAddress;

                if (!context.Settings.TryGet("PublicReturnAddress",out replyToAddress))
                {
                    replyToAddress = context.Settings.LocalAddress();
                }

                context.Container.ConfigureComponent(b => new ApplyReplyToAddressBehavior(replyToAddress), DependencyLifecycle.SingleInstance);
            }
     
            var router = SetupStaticRouter(context);
            context.Container.RegisterSingleton(router);

            context.Container.ConfigureComponent(b => new DetermineRouteForSendBehavior(context.Settings.LocalAddress(),
                new RoutingAdapter(router)), DependencyLifecycle.InstancePerCall);

            if (!context.Settings.Get<TransportDefinition>().HasNativePubSubSupport)
            {
                context.Container.ConfigureComponent<DispatchStrategy>(b => new StorageDrivenDispatcher(b.Build<ISubscriptionStorage>(), b.Build<MessageMetadataRegistry>()), DependencyLifecycle.SingleInstance);
            }
            else
            {
                context.Container.ConfigureComponent<DispatchStrategy>(b => new DefaultDispatchStrategy(), DependencyLifecycle.SingleInstance);
            }
        }

        static StaticMessageRouter SetupStaticRouter(FeatureConfigurationContext context)
        {
            var conventions = context.Settings.Get<Conventions>();

            var knownMessages = context.Settings.GetAvailableTypes()
                .Where(conventions.IsMessageType)
                .ToList();

            var unicastConfig = context.Settings.GetConfigSection<UnicastBusConfig>();
            var router = new StaticMessageRouter(knownMessages);

            if (unicastConfig != null)
            {
                var messageEndpointMappings = unicastConfig.MessageEndpointMappings.Cast<MessageEndpointMapping>()
                    .OrderByDescending(m => m)
                    .ToList();

                foreach (var mapping in messageEndpointMappings)
                {
                    mapping.Configure((messageType, address) =>
                    {
                        if (!(conventions.IsMessageType(messageType) || conventions.IsEventType(messageType) || conventions.IsCommandType(messageType)))
                        {
                            return;
                        }

                        if (conventions.IsEventType(messageType))
                        {
                            router.RegisterEventRoute(messageType, address);
                            return;
                        }

                        router.RegisterMessageRoute(messageType, address);
                    });
                }
            }

            return router;
        }
    }
}