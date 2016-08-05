namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using Config;
    using Routing;
    using Routing.MessageDrivenSubscriptions;
    using Transport;
    using Unicast.Messages;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

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
                s.SetDefault<ConfiguredUnicastRoutes>(new ConfiguredUnicastRoutes());
                s.SetDefault<ConfiguredPublishers>(new ConfiguredPublishers());
            });
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var canReceive = !context.Settings.GetOrDefault<bool>("Endpoint.SendOnly");
            var transportInfrastructure = context.Settings.Get<TransportInfrastructure>();

            var unicastRoutingTable = context.Settings.Get<UnicastRoutingTable>();
            var endpointInstances = context.Settings.Get<EndpointInstances>();
            var publishers = context.Settings.Get<Publishers>();
            var distributionPolicy = context.Settings.Get<DistributionPolicy>();
            var configuredUnicastRoutes = context.Settings.Get<ConfiguredUnicastRoutes>();
            var configuredPublishers = context.Settings.Get<ConfiguredPublishers>();
            var conventions = context.Settings.Get<Conventions>();

            var unicastBusConfig = context.Settings.GetConfigSection<UnicastBusConfig>();
            if (unicastBusConfig != null)
            {
                ImportMessageEndpointMappings(unicastBusConfig.MessageEndpointMappings, transportInfrastructure, publishers, unicastRoutingTable);
            }

            configuredUnicastRoutes.Apply(unicastRoutingTable);

            foreach (var registration in configuredPublishers)
            {
                registration(publishers, conventions);
            }

            var outboundRoutingPolicy = transportInfrastructure.OutboundRoutingPolicy;
            context.Pipeline.Register(b =>
            {
                var unicastSendRouter = new UnicastSendRouter(unicastRoutingTable, endpointInstances, i => transportInfrastructure.ToTransportAddress(new LogicalAddress(i)));
                return new UnicastSendRouterConnector(context.Settings.LocalAddress(), context.Settings.InstanceSpecificQueue(), unicastSendRouter, distributionPolicy);
            }, "Determines how the message being sent should be routed");

            context.Pipeline.Register(new UnicastReplyRouterConnector(), "Determines how replies should be routed");
            if (outboundRoutingPolicy.Publishes == OutboundRoutingType.Unicast)
            {
                context.Pipeline.Register(b =>
                {
                    var unicastPublishRouter = new UnicastPublishRouter(b.Build<MessageMetadataRegistry>(), b.Build<ISubscriptionStorage>(), i => transportInfrastructure.ToTransportAddress(new LogicalAddress(i)));
                    return new UnicastPublishRouterConnector(unicastPublishRouter, distributionPolicy);
                }, "Determines how the published messages should be routed");
            }
            else
            {
                context.Pipeline.Register(new MulticastPublishRouterBehavior(), "Determines how the published messages should be routed");
            }

            if (canReceive)
            {
                var publicReturnAddress = context.Settings.GetOrDefault<string>("PublicReturnAddress");
                var distributorAddress = context.Settings.GetOrDefault<string>("LegacyDistributor.Address");
                context.Pipeline.Register(new ApplyReplyToAddressBehavior(context.Settings.LocalAddress(), context.Settings.InstanceSpecificQueue(), publicReturnAddress, distributorAddress), "Applies the public reply to address to outgoing messages");

                if (outboundRoutingPolicy.Publishes == OutboundRoutingType.Unicast)
                {
                    var subscriberAddress = distributorAddress ?? context.Settings.LocalAddress();
                    var subscriptionRouter = new SubscriptionRouter(publishers, endpointInstances, i => transportInfrastructure.ToTransportAddress(new LogicalAddress(i)));

                    context.Pipeline.Register(b => new MessageDrivenSubscribeTerminator(subscriptionRouter, subscriberAddress, context.Settings.EndpointName(), b.Build<IDispatchMessages>()), "Sends subscription requests when message driven subscriptions is in use");
                    context.Pipeline.Register(b => new MessageDrivenUnsubscribeTerminator(subscriptionRouter, subscriberAddress, context.Settings.EndpointName(), b.Build<IDispatchMessages>()), "Sends requests to unsubscribe when message driven subscriptions is in use");
                }
                else
                {
                    var transportSubscriptionInfrastructure = transportInfrastructure.ConfigureSubscriptionInfrastructure();
                    var subscriptionManager = transportSubscriptionInfrastructure.SubscriptionManagerFactory();

                    context.Pipeline.Register(new NativeSubscribeTerminator(subscriptionManager), "Requests the transport to subscribe to a given message type");
                    context.Pipeline.Register(new NativeUnsubscribeTerminator(subscriptionManager), "Requests the transport to unsubscribe to a given message type");
                }
            }
        }

        static void ImportMessageEndpointMappings(MessageEndpointMappingCollection legacyRoutingConfig, TransportInfrastructure transportInfrastructure, Publishers publishers, UnicastRoutingTable unicastRoutingTable)
        {
            var entries = new Dictionary<Type, RouteTableEntry>();

            foreach (MessageEndpointMapping m in legacyRoutingConfig)
            {
                m.Configure((type, address) =>
                {
                    entries[type] = new RouteTableEntry(type, UnicastRoute.CreateFromPhysicalAddress(transportInfrastructure.MakeCanonicalForm(address)));
                    publishers.AddByAddress(type, address);
                });
            }

            unicastRoutingTable.AddOrReplaceRoutes("MessageEndpointMappings", entries.Values.ToList());
        }
    }

    class ConfiguredUnicastRoutes
    {
        List<IRouteSource> routeSources = new List<IRouteSource>();

        public void Add(IRouteSource routeSource)
        {
            Guard.AgainstNull(nameof(routeSource), routeSource);
            routeSources.Add(routeSource);
        }

        public void Apply(UnicastRoutingTable unicastRoutingTable)
        {
            var entries = new Dictionary<Type, RouteTableEntry>();
            foreach (var source in routeSources.OrderBy(x => x.Priority)) //Higher priority routes sources override lowe priority.
            {
                source.GenerateRoutes(e => entries[e.MessageType] = e);
            }
            unicastRoutingTable.AddOrReplaceRoutes("EndpointConfiguration", entries.Values.ToList());
        }
    }

    class ConfiguredPublishers : List<Action<Publishers, Conventions>>
    {
    }
}