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

            var unicastRoutingTable = context.Settings.Get<UnicastRoutingTable>();
            var endpointInstances = context.Settings.Get<EndpointInstances>();
            var publishers = context.Settings.Get<Publishers>();
            var distributionPolicy = context.Settings.Get<DistributionPolicy>();
            var transportAddresses = context.Settings.Get<TransportAddresses>();
            var messageMetadataRegistry = context.Settings.Get<MessageMetadataRegistry>();

            var unicastBusConfig = context.Settings.GetConfigSection<UnicastBusConfig>();
            if (unicastBusConfig != null)
            {
                ImportMessageEndpointMappings(context, unicastBusConfig.MessageEndpointMappings, transportInfrastructure, publishers, unicastRoutingTable);
            }

            var outboundRoutingPolicy = transportInfrastructure.OutboundRoutingPolicy;
            var unicastSendRouter = new UnicastSendRouter(messageMetadataRegistry, unicastRoutingTable, endpointInstances, transportAddresses);
            context.Pipeline.Register("UnicastSendRouterConnector", new UnicastSendRouterConnector(context.Settings.LocalAddress(), context.Settings.InstanceSpecificQueue(), unicastSendRouter, distributionPolicy), "Determines how the message being sent should be routed");
            context.Pipeline.Register("UnicastReplyRouterConnector", new UnicastReplyRouterConnector(), "Determines how replies should be routed");
            if (outboundRoutingPolicy.Publishes == OutboundRoutingType.Unicast)
            {
                context.Pipeline.Register("UnicastPublishRouterConnector", b =>
                {
                    var unicastPublishRouter = new UnicastPublishRouter(messageMetadataRegistry, b.Build<ISubscriptionStorage>(), endpointInstances, transportAddresses);
                    return new UnicastPublishRouterConnector(unicastPublishRouter, distributionPolicy);
                }, "Determines how the published messages should be routed");
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

        static void ImportMessageEndpointMappings(FeatureConfigurationContext context, MessageEndpointMappingCollection legacyRoutingConfig, TransportInfrastructure transportInfrastructure, Publishers publishers, UnicastRoutingTable unicastRoutingTable)
        {
            var conventions = context.Settings.Get<Conventions>();

            var knownMessageTypes = context.Settings.GetAvailableTypes()
                .Where(conventions.IsMessageType)
                .ToList();

            foreach (MessageEndpointMapping m in legacyRoutingConfig)
            {
                m.Configure((type, s) =>
                {
                    ConfigureSendDestination(transportInfrastructure, unicastRoutingTable, type, s);
                    ConfigureSubscribeDestination(publishers, knownMessageTypes, type, s);
                });
            }
        }

        static void ConfigureSubscribeDestination(Publishers publishers, List<Type> knownMessageTypes, Type type, string s)
        {
            var typesEnclosed = knownMessageTypes.Where(t => t.IsAssignableFrom(type));
            foreach (var t in typesEnclosed)
            {
                publishers.AddByAddress(s, t);
            }
        }

        static void ConfigureSendDestination(TransportInfrastructure transportInfrastructure, UnicastRoutingTable unicastRoutingTable, Type type, string s)
        {
            unicastRoutingTable.RouteToAddress(type, transportInfrastructure.MakeCanonicalForm(s));
        }
    }
}