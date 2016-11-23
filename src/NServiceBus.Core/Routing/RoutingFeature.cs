namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Config;
    using Routing;
    using Transport;

    class RoutingFeature : Feature
    {
        public const string EnforceBestPracticesSettingsKey = "NServiceBus.Routing.EnforceBestPractices";

        public RoutingFeature()
        {
            EnableByDefault();
            Defaults(s =>
            {
                s.SetDefault<UnicastRoutingTable>(new UnicastRoutingTable());
                s.SetDefault<UnicastSubscriberTable>(new UnicastSubscriberTable());
                s.SetDefault<ConfiguredUnicastRoutes>(new ConfiguredUnicastRoutes());

                s.SetDefault<EndpointInstances>(new EndpointInstances());
                s.SetDefault<DistributionPolicy>(new DistributionPolicy());

                s.SetDefault(EnforceBestPracticesSettingsKey, true);

                var routingComponent = new RoutingComponent(s);
                s.SetDefault<IRoutingComponent>(routingComponent);
                s.SetDefault<RoutingComponent>(routingComponent);
            });
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var canReceive = !context.Settings.GetOrDefault<bool>("Endpoint.SendOnly");
            var transportInfrastructure = context.Settings.Get<TransportInfrastructure>();
            var conventions = context.Settings.Get<Conventions>();
            var unicastBusConfig = context.Settings.GetConfigSection<UnicastBusConfig>();
            var routing = context.Settings.Get<RoutingComponent>();

            var distributionPolicy = context.Settings.Get<DistributionPolicy>();
            var configuredUnicastRoutes = context.Settings.Get<ConfiguredUnicastRoutes>();

            if (context.Settings.Get<bool>(EnforceBestPracticesSettingsKey))
            {
                EnableBestPracticeEnforcement(context);
            }

            ApplyLegacyConfiguration(unicastBusConfig?.MessageEndpointMappings, routing.Sending, transportInfrastructure.MakeCanonicalForm, conventions);
            configuredUnicastRoutes.Apply(routing.Sending, conventions);

            Func<EndpointInstance, string> addressTranslation = i => transportInfrastructure.ToTransportAddress(LogicalAddress.CreateRemoteAddress(i));

            var unicastSendRouter = new UnicastSendRouter(routing.Sending, routing.EndpointInstances, addressTranslation);
            context.Pipeline.Register(new UnicastSendRouterConnector(context.Settings.LocalAddress(), context.Settings.InstanceSpecificQueue(), unicastSendRouter, distributionPolicy, addressTranslation),
                "Determines how the message being sent should be routed");

            if (transportInfrastructure.OutboundRoutingPolicy.Publishes == OutboundRoutingType.Unicast)
            {
                var unicastPublishRouter = new UnicastPublishRouter(routing.Publishing, routing.EndpointInstances, addressTranslation);
                context.Pipeline.Register(new UnicastPublishRouterConnector(unicastPublishRouter, distributionPolicy), 
                    "Determines how the published messages should be routed");
            }

            context.Pipeline.Register(new UnicastReplyRouterConnector(), "Determines how replies should be routed");

            if (canReceive)
            {
                var publicReturnAddress = context.Settings.GetOrDefault<string>("PublicReturnAddress");
                var distributorAddress = context.Settings.GetOrDefault<string>("LegacyDistributor.Address");
                context.Pipeline.Register(new ApplyReplyToAddressBehavior(context.Settings.LocalAddress(), context.Settings.InstanceSpecificQueue(), publicReturnAddress, distributorAddress), 
                    "Applies the public reply to address to outgoing messages");


                context.Pipeline.Register(b => new SubscribeTerminator(routing, b), "Calls handlers for the subscribe requests.");
                context.Pipeline.Register(b => new UnsubscribeTerminator(routing, b), "Calls handlers for the unsubscribe requests.");
            }
        }

        void EnableBestPracticeEnforcement(FeatureConfigurationContext context)
        {
            var validations = new Validations(context.Settings.Get<Conventions>());

            context.Pipeline.Register(
                "EnforceSendBestPractices",
                new EnforceSendBestPracticesBehavior(validations),
                "Enforces send messaging best practices");

            context.Pipeline.Register(
                "EnforceReplyBestPractices",
                new EnforceReplyBestPracticesBehavior(validations),
                "Enforces reply messaging best practices");

            context.Pipeline.Register(
                "EnforcePublishBestPractices",
                new EnforcePublishBestPracticesBehavior(validations),
                "Enforces publish messaging best practices");

            context.Pipeline.Register(
                "EnforceSubscribeBestPractices",
                new EnforceSubscribeBestPracticesBehavior(validations),
                "Enforces subscribe messaging best practices");

            context.Pipeline.Register(
                "EnforceUnsubscribeBestPractices",
                new EnforceUnsubscribeBestPracticesBehavior(validations),
                "Enforces unsubscribe messaging best practices");
        }

        internal static void ApplyLegacyConfiguration(MessageEndpointMappingCollection mappings, UnicastRoutingTable unicastRoutingTable, Func<string, string> makeCanonicalAddress, Conventions conventions)
        {
            if (mappings == null)
            {
                return;
            }
            var routeTableEntries = new Dictionary<Type, RouteTableEntry>();

            mappings.Apply(makeCanonicalAddress, (type, address, baseTypes) =>
            {
                var route = UnicastRoute.CreateFromPhysicalAddress(address);
                foreach (var baseType in baseTypes)
                {
                    routeTableEntries[baseType] = new RouteTableEntry(baseType, route);
                }
                routeTableEntries[type] = new RouteTableEntry(type, route);
            }, conventions);

            unicastRoutingTable.AddOrReplaceRoutes("MessageEndpointMappings", routeTableEntries.Values.ToList());
        }
    }
}