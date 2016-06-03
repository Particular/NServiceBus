﻿namespace NServiceBus.Features
{
    using System.Linq;
    using Config;
    using Routing;
    using Routing.MessageDrivenSubscriptions;
    using Transports;

    class RoutingFeature : Feature
    {
        public RoutingFeature()
        {
            EnableByDefault();
            Defaults(s =>
            {
                s.SetDefault<UnicastRoutingTable>(new UnicastRoutingTable());
                s.SetDefault<EndpointInstances>(new EndpointInstances());
                s.SetDefault<Publishers>(new Publishers());
                s.SetDefault<DistributionPolicy>(new DistributionPolicy());
            });
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var canReceive = !context.Settings.GetOrDefault<bool>("Endpoint.SendOnly");
            var transportInfrastructure = context.Settings.Get<TransportInfrastructure>();

            context.Container.ConfigureComponent(b => context.Settings.Get<UnicastRoutingTable>(), DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent(b => context.Settings.Get<EndpointInstances>(), DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent(b => context.Settings.Get<Publishers>(), DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent(b => context.Settings.Get<DistributionPolicy>(), DependencyLifecycle.SingleInstance);

            var unicastBusConfig = context.Settings.GetConfigSection<UnicastBusConfig>();
            if (unicastBusConfig != null)
            {
                var routeTable = context.Settings.Get<UnicastRoutingTable>();
                var publishers = context.Settings.Get<Publishers>();
                var legacyRoutingConfig = unicastBusConfig.MessageEndpointMappings;
                var conventions = context.Settings.Get<Conventions>();

                var knownMessageTypes = context.Settings.GetAvailableTypes()
                    .Where(conventions.IsMessageType)
                    .ToList();

                foreach (MessageEndpointMapping m in legacyRoutingConfig)
                {
                    m.Configure((type, s) => routeTable.RouteToAddress(type, transportInfrastructure.MakeCanonicalForm(s)));
                    m.Configure((type, s) =>
                    {
                        var typesEnclosed = knownMessageTypes.Where(t => t.IsAssignableFrom(type));
                        foreach (var t in typesEnclosed)
                        {
                            publishers.AddByAddress(s, t);
                        }
                    });
                }
            }

            var outboundRoutingPolicy = transportInfrastructure.OutboundRoutingPolicy;
            context.Container.ConfigureComponent<UnicastSendRouter>(DependencyLifecycle.SingleInstance);
            context.Pipeline.Register(b => new UnicastSendRouterConnector(context.Settings.LocalAddress(), context.Settings.InstanceSpecificQueue(), b.Build<UnicastSendRouter>(), b.Build<DistributionPolicy>()), "Determines how the message being sent should be routed");
            context.Pipeline.Register(typeof(UnicastReplyRouterConnector), "Determines how replies should be routed");
            if (outboundRoutingPolicy.Publishes == OutboundRoutingType.Unicast)
            {
                context.Container.ConfigureComponent<UnicastPublishRouter>(DependencyLifecycle.SingleInstance);
                context.Pipeline.Register(b => new UnicastPublishRouterConnector(b.Build<UnicastPublishRouter>(), b.Build<DistributionPolicy>()), "Determines how the published messages should be routed");
            }
            else
            {
                context.Pipeline.Register(typeof(MulticastPublishRouterBehavior), "Determines how the published messages should be routed");
            }

            if (canReceive)
            {
                var publicReturnAddress = context.Settings.GetOrDefault<string>("PublicReturnAddress");
                var distributorAddress = context.Settings.GetOrDefault<string>("LegacyDistributor.Address");
                context.Pipeline.Register("ApplyReplyToAddress", new ApplyReplyToAddressBehavior(context.Settings.LocalAddress(), context.Settings.InstanceSpecificQueue(), publicReturnAddress, distributorAddress), "Applies the public reply to address to outgoing messages");

                if (outboundRoutingPolicy.Publishes == OutboundRoutingType.Unicast)
                {
                    context.Container.ConfigureComponent<SubscriptionRouter>(DependencyLifecycle.SingleInstance);

                    context.Pipeline.Register(typeof(MessageDrivenSubscribeTerminator), "Sends subscription requests when message driven subscriptions is in use");
                    context.Pipeline.Register(typeof(MessageDrivenUnsubscribeTerminator), "Sends requests to unsubscribe when message driven subscriptions is in use");

                    var subscriberAddress = distributorAddress ?? context.Settings.LocalAddress();
                    context.Container.ConfigureComponent(b => new MessageDrivenSubscribeTerminator(b.Build<SubscriptionRouter>(), subscriberAddress, context.Settings.EndpointName(), b.Build<IDispatchMessages>()), DependencyLifecycle.SingleInstance);
                    context.Container.ConfigureComponent(b => new MessageDrivenUnsubscribeTerminator(b.Build<SubscriptionRouter>(), subscriberAddress, context.Settings.EndpointName(), b.Build<IDispatchMessages>()), DependencyLifecycle.SingleInstance);
                }
                else
                {
                    var transportSubscriptionInfrastructure = transportInfrastructure.ConfigureSubscriptionInfrastructure();
                    var subscriptionManager = transportSubscriptionInfrastructure.SubscriptionManagerFactory();

                    context.Container.RegisterSingleton(subscriptionManager);
                    context.Pipeline.Register(typeof(NativeSubscribeTerminator), "Requests the transport to subscribe to a given message type");
                    context.Pipeline.Register(typeof(NativeUnsubscribeTerminator), "Requests the transport to unsubscribe to a given message type");
                }
            }
        }
    }
}