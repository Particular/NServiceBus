namespace NServiceBus.Features
{
    using Settings;
    using Transport;
    using Unicast.Messages;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    class HybridSubscriptions : Feature
    {
        public HybridSubscriptions()
        {
            EnableByDefault();
            Prerequisite(c => c.Settings.Get<TransportInfrastructure>().OutboundRoutingPolicy.Publishes == OutboundRoutingType.Multicast, "The transport does not support native pub sub");
            Prerequisite(c => IsMigrationModeEnabled(c.Settings), "The transport has not enabled pub sub migration mode");
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var transportInfrastructure = context.Settings.Get<TransportInfrastructure>();
            var canReceive = !context.Settings.GetOrDefault<bool>("Endpoint.SendOnly");

            var distributionPolicy = context.Routing.DistributionPolicy;
            var publishers = context.Routing.Publishers;
            var configuredPublishers = context.Settings.Get<ConfiguredPublishers>();
            var conventions = context.Settings.Get<Conventions>();
            var enforceBestPractices = context.Routing.EnforceBestPractices;

            configuredPublishers.Apply(publishers, conventions, enforceBestPractices);

            context.Pipeline.Register(b =>
            {
                var unicastPublishRouter = new UnicastPublishRouter(b.Build<MessageMetadataRegistry>(), i => transportInfrastructure.ToTransportAddress(LogicalAddress.CreateRemoteAddress(i)), b.Build<ISubscriptionStorage>());
                return new HybridRouterConnector(distributionPolicy, unicastPublishRouter);
            }, "Determines how the published messages should be routed");

            if (canReceive)
            {
                var endpointInstances = context.Routing.EndpointInstances;
                var transportSubscriptionInfrastructure = transportInfrastructure.ConfigureSubscriptionInfrastructure();
                var subscriptionManager = transportSubscriptionInfrastructure.SubscriptionManagerFactory();

                var subscriptionRouter = new SubscriptionRouter(publishers, endpointInstances, i => transportInfrastructure.ToTransportAddress(LogicalAddress.CreateRemoteAddress(i)));
                var subscriberAddress = context.Receiving.LocalAddress;

                context.Pipeline.Register(b =>
                    new HybridSubscribeTerminator(subscriptionManager, subscriptionRouter, b.Build<IDispatchMessages>(), subscriberAddress, context.Settings.EndpointName()), "Requests the transport to subscribe to a given message type");
                context.Pipeline.Register(b =>
                    new HybridUnsubscribeTerminator(subscriptionManager,subscriptionRouter, b.Build<IDispatchMessages>(), subscriberAddress, context.Settings.EndpointName()), "Sends requests to unsubscribe when message driven subscriptions is in use");

                var authorizer = context.Settings.GetSubscriptionAuthorizer();
                if (authorizer == null)
                {
                    authorizer = _ => true;
                }
                context.Container.RegisterSingleton(authorizer);
                context.Pipeline.Register<SubscriptionReceiverBehavior.Registration>();
            }
        }

        public static bool IsMigrationModeEnabled(ReadOnlySettings settings)
        {
            // this key can be set by transports once they provide native support for pub/sub.
            return settings.TryGet("NServiceBus.Subscriptions.EnableMigrationMode", out bool enabled) && enabled;
        }
    }
}