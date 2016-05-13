namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Config;
    using Routing;
    using Routing.MessageDrivenSubscriptions;
    using Transports;
    using Unicast.Messages;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    class RoutingFeature : Feature
    {
        public const string RealoadTableIntervalSettingsKey = "NServiceBus.Routing.ReloadInterval";

        public RoutingFeature()
        {
            EnableByDefault();
            Defaults(s =>
            {
                s.SetDefault(RealoadTableIntervalSettingsKey, TimeSpan.FromSeconds(10));

                s.SetDefault<UnicastRoutingTableConfiguration>(new UnicastRoutingTableConfiguration());
                s.SetDefault<EndpointInstances>(new EndpointInstances());
                s.SetDefault<Publishers>(new Publishers());                
                s.SetDefault<DistributionPolicy>(new DistributionPolicy());
            });
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var tableReloadInterval = context.Settings.Get<TimeSpan>(RealoadTableIntervalSettingsKey);

            var canReceive = !context.Settings.GetOrDefault<bool>("Endpoint.SendOnly");
            var transportInfrastructure = context.Settings.Get<TransportInfrastructure>();
            var messageMetadataRegistry = context.Settings.Get<MessageMetadataRegistry>();
            var unicastRoutingTableConfig = context.Settings.Get<UnicastRoutingTableConfiguration>();
            var endpointInstances = context.Settings.Get<EndpointInstances>();
            var publishers = context.Settings.Get<Publishers>();
            var transportAddresses = context.Settings.Get<TransportAddresses>();
            var distributionPolicy = context.Settings.Get<DistributionPolicy>();
            var conventions = context.Settings.Get<Conventions>();
            var knownMessageTypes = context.Settings.GetAvailableTypes()
                .Where(conventions.IsMessageType)
                .ToList();

            context.RegisterStartupTask(b => new UnicastRouterTableUpdater(new AsyncTimer(), b.BuildAll<UnicastRouter>().ToList(), tableReloadInterval));

            ImportMessageEndpointMappings(context, transportInfrastructure, knownMessageTypes);

            var outboundRoutingPolicy = transportInfrastructure.OutboundRoutingPolicy;

            context.Container.ConfigureComponent(b => new UnicastSendRouter("sending", messageMetadataRegistry, unicastRoutingTableConfig, endpointInstances, transportAddresses, distributionPolicy, knownMessageTypes), DependencyLifecycle.SingleInstance);
            context.Pipeline.Register("UnicastSendRouterConnector", b => new UnicastSendRouterConnector(context.Settings.LocalAddress(), context.Settings.InstanceSpecificQueue(), b.Build<UnicastSendRouter>()), "Determines how the message being sent should be routed");
            context.Pipeline.Register("UnicastReplyRouterConnector", new UnicastReplyRouterConnector(), "Determines how replies should be routed");

            if (outboundRoutingPolicy.Publishes == OutboundRoutingType.Unicast)
            {
                context.Container.ConfigureComponent(b => new UnicastPublishRouter("publishing", messageMetadataRegistry, b.Build<ISubscriptionStorage>(), endpointInstances, transportAddresses, distributionPolicy, knownMessageTypes), DependencyLifecycle.SingleInstance);
                context.Pipeline.Register("UnicastPublishRouterConnector", b => new UnicastPublishRouterConnector(b.Build<UnicastPublishRouter>()), "Determines how the published messages should be routed");
            }
            else
            {
                context.Pipeline.Register("MulticastPublishRouterBehavior", new MulticastPublishRouterBehavior(), "Determines how the published messages should be routed");
            }

            if (canReceive)
            {
                var publicReturnAddress = context.Settings.GetOrDefault<string>("PublicReturnAddress");
                var distributorAddress = context.Settings.GetOrDefault<string>("LegacyDistributor.Address");
                context.Pipeline.Register("ApplyReplyToAddress", new ApplyReplyToAddressBehavior(context.Settings.LocalAddress(), context.Settings.InstanceSpecificQueue(), publicReturnAddress, distributorAddress), "Applies the public reply to address to outgoing messages");

                if (outboundRoutingPolicy.Publishes == OutboundRoutingType.Unicast)
                {
                    var subscriberAddress = distributorAddress ?? context.Settings.LocalAddress();
                    var subscriptionRouter = new SubscriptionRouter(publishers, endpointInstances, transportAddresses);

                    context.Pipeline.Register("MessageDrivenSubscribeTerminator", b => new MessageDrivenSubscribeTerminator(subscriptionRouter, subscriberAddress, context.Settings.EndpointName(), b.Build<IDispatchMessages>()), "Sends subscription requests when message driven subscriptions is in use");
                    context.Pipeline.Register("MessageDrivenUnsubscribeTerminator", b => new MessageDrivenUnsubscribeTerminator(subscriptionRouter, subscriberAddress, context.Settings.EndpointName(), b.Build<IDispatchMessages>()), "Sends requests to unsubscribe when message driven subscriptions is in use");
                }
                else
                {
                    var transportSubscriptionInfrastructure = transportInfrastructure.ConfigureSubscriptionInfrastructure();
                    var subscriptionManager = transportSubscriptionInfrastructure.SubscriptionManagerFactory();

                    context.Pipeline.Register("NativeSubscribeTerminator", new NativeSubscribeTerminator(subscriptionManager), "Requests the transport to subscribe to a given message type");
                    context.Pipeline.Register("NativeUnsubscribeTerminator", new NativeUnsubscribeTerminator(subscriptionManager), "Requests the transport to unsubscribe to a given message type");
                }
            }
        }

        static void ImportMessageEndpointMappings(FeatureConfigurationContext context, TransportInfrastructure transportInfrastructure, List<Type> knownMessageTypes)
        {
            var unicastBusConfig = context.Settings.GetConfigSection<UnicastBusConfig>();
            if (unicastBusConfig != null)
            {
                var routeTable = context.Settings.Get<UnicastRoutingTableConfiguration>();
                var publishers = context.Settings.Get<Publishers>();
                var legacyRoutingConfig = unicastBusConfig.MessageEndpointMappings;

                foreach (MessageEndpointMapping m in legacyRoutingConfig)
                {
                    m.Configure((type, s) =>
                    {
                        ConfigureSendDestination(routeTable, type, transportInfrastructure, s);
                        ConfigureSubscribeDestination(knownMessageTypes, type, publishers, s);
                    });
                }
            }
        }

        static void ConfigureSubscribeDestination(List<Type> knownMessageTypes, Type type, Publishers publishers, string s)
        {
            var typesEnclosed = knownMessageTypes.Where(t => t.IsAssignableFrom(type));
            foreach (var t in typesEnclosed)
            {
                publishers.Add(s, t);
            }
        }

        static void ConfigureSendDestination(UnicastRoutingTableConfiguration routeTable, Type type, TransportInfrastructure transportInfrastructure, string s)
        {
            routeTable.RouteToAddress(type, transportInfrastructure.MakeCanonicalForm(s));
        }
    }
}