namespace NServiceBus.Features
{
    using System.Linq;
    using NServiceBus.Config;
    using NServiceBus.Routing;
    using NServiceBus.Routing.MessageDrivenSubscriptions;
    using NServiceBus.Routing.StorageDrivenPublishing;
    using NServiceBus.Settings;
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
            var canReceive = !context.Settings.GetOrDefault<bool>("Endpoint.SendOnly");
            var transportDefinition = context.Settings.Get<TransportDefinition>();
            var staticRoutes = InitializeStaticRoutes(context.Settings);

            context.Container.ConfigureComponent(b => new DetermineRouteForSendBehavior(context.Settings.LocalAddress(),
               new DefaultMessageRouter(staticRoutes)), DependencyLifecycle.InstancePerCall);

            if (transportDefinition.HasNativePubSubSupport)
            {
                context.Container.ConfigureComponent<DispatchStrategy>(b => new DefaultDispatchStrategy(), DependencyLifecycle.SingleInstance);
            }
            else
            {
                context.Container.ConfigureComponent<DispatchStrategy>(b => new StorageDrivenDispatcher(b.Build<IQuerySubscriptions>(), b.Build<MessageMetadataRegistry>()), DependencyLifecycle.SingleInstance);
            }

            context.Pipeline.Register("DetermineRouteForSend", typeof(DetermineRouteForSendBehavior), "Determines how the message being sent should be routed");
            context.Pipeline.Register("DetermineRouteForReply", typeof(DetermineRouteForReplyBehavior), "Determines how replies should be routed");
            context.Pipeline.Register("DetermineRouteForPublish", typeof(DetermineRouteForPublishBehavior), "Determines how the published messages should be routed");


            if (canReceive)
            {
                context.Pipeline.Register("ApplyReplyToAddress", typeof(ApplyReplyToAddressBehavior), "Applies the public reply to address to outgoing messages");

                string replyToAddress;

                if (!context.Settings.TryGet("PublicReturnAddress", out replyToAddress))
                {
                    replyToAddress = context.Settings.LocalAddress();
                }

                context.Container.ConfigureComponent(b => new ApplyReplyToAddressBehavior(replyToAddress), DependencyLifecycle.SingleInstance);

                if (transportDefinition.HasNativePubSubSupport)
                {
                    context.Container.RegisterSingleton(transportDefinition.GetSubscriptionManager());
                    
                    context.Pipeline.Register("NativeSubscribeTerminator", typeof(NativeSubscribeTerminator), "Requests the transport to subscribe to a given message type");
                    context.Pipeline.Register("NativeUnsubscribeTerminator", typeof(NativeUnsubscribeTerminator), "Requests the transport to unsubscribe to a given message type");
                }
                else
                {
                    var conventions = context.Settings.Get<Conventions>();

                    var knownMessageTypes = context.Settings.GetAvailableTypes()
                        .Where(conventions.IsMessageType)
                        .ToList();

                    var subscriptionRouter = new SubscriptionRouter(staticRoutes, knownMessageTypes);

                    context.Container.RegisterSingleton(subscriptionRouter); //needed by the autosubscriptions<

                    context.Pipeline.Register("MessageDrivenSubscribeTerminator", typeof(MessageDrivenSubscribeTerminator), "Sends subscription requests when message driven subscriptions is in use");
                    context.Pipeline.Register("MessageDrivenUnsubscribeTerminator", typeof(MessageDrivenUnsubscribeTerminator), "Sends requests to unsubscribe when message driven subscriptions is in use");

                    context.Container.ConfigureComponent(b => new MessageDrivenSubscribeTerminator(subscriptionRouter, replyToAddress, b.Build<IDispatchMessages>()), DependencyLifecycle.SingleInstance);
                    context.Container.ConfigureComponent(b => new MessageDrivenUnsubscribeTerminator(subscriptionRouter, replyToAddress, b.Build<IDispatchMessages>()), DependencyLifecycle.SingleInstance);
                }

            }
        }

        static StaticRoutes InitializeStaticRoutes(ReadOnlySettings settings)
        {
            var routes = new StaticRoutes();
            var unicastConfig = settings.GetConfigSection<UnicastBusConfig>();

            if (unicastConfig == null)
            {
                return routes;
            }

            var messageEndpointMappings = unicastConfig.MessageEndpointMappings.Cast<MessageEndpointMapping>()
                .OrderByDescending(m => m)
                .ToList();

            foreach (var mapping in messageEndpointMappings)
            {
                mapping.Configure((messageType, address) =>
                {
                    routes.Register(messageType, address);
                });
            }


            return routes;
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