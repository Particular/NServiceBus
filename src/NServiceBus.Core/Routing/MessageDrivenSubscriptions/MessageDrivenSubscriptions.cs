namespace NServiceBus.Features
{
    using System;
    using Persistence;
    using Routing;
    using Routing.MessageDrivenSubscriptions;
    using Transport;
    using Unicast.Messages;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    /// <summary>
    /// Allows subscribers to register by sending a subscription message to this endpoint.
    /// </summary>
    public class MessageDrivenSubscriptions : Feature
    {
        internal MessageDrivenSubscriptions()
        {
            EnableByDefault();
            DependsOn<RoutingFeature>();
            Defaults(s =>
            {
                // s.SetDefault<Publishers>(new Publishers()); currently setup by RoutingFeature
                s.SetDefault<ConfiguredPublishers>(new ConfiguredPublishers());
            });
            Prerequisite(c => c.Settings.Get<TransportInfrastructure>().OutboundRoutingPolicy.Publishes == OutboundRoutingType.Unicast, "The transport supports native pub sub");
        }

        /// <summary>
        /// See <see cref="Feature.Setup" />.
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            if (!PersistenceStartup.HasSupportFor<StorageType.Subscriptions>(context.Settings))
            {
                throw new Exception("The selected persistence doesn't have support for subscription storage. Select another persistence or disable the message-driven subscriptions feature using endpointConfiguration.DisableFeature<MessageDrivenSubscriptions>()");
            }

            var transportInfrastructure = context.Settings.Get<TransportInfrastructure>();
            var canReceive = !context.Settings.GetOrDefault<bool>("Endpoint.SendOnly");
            var conventions = context.Settings.Get<Conventions>();
            var enforceBestPractices = context.Settings.Get<bool>(RoutingFeature.EnforceBestPracticesSettingsKey);

            var distributionPolicy = context.Settings.Get<DistributionPolicy>();
            var endpointInstances = context.Settings.Get<EndpointInstances>();
            var publishers = context.Settings.Get<Publishers>();
            var configuredPublishers = context.Settings.Get<ConfiguredPublishers>();

            configuredPublishers.Apply(publishers, conventions, enforceBestPractices);

            var subscriptionRouter = new SubscriptionRouter(publishers, endpointInstances, i => transportInfrastructure.ToTransportAddress(LogicalAddress.CreateRemoteAddress(i)));
            var distributorAddress = context.Settings.GetOrDefault<string>("LegacyDistributor.Address");
            var subscriberAddress = distributorAddress ?? context.Settings.LocalAddress();

            context.Container.ConfigureComponent<IUnicastPublishSubscribe>(b =>
            {
                var unicastPublishRouter = new UnicastPublishRouter(b.Build<MessageMetadataRegistry>(), b.Build<ISubscriptionStorage>());
                return new MessageDrivenPublishSubscribe(subscriptionRouter, subscriberAddress, context.Settings.EndpointName(), b.Build<IDispatchMessages>(), unicastPublishRouter, distributionPolicy);
            }, DependencyLifecycle.SingleInstance);

            if (canReceive)
            {
                var authorizer = context.Settings.GetSubscriptionAuthorizer();
                if (authorizer == null)
                {
                    authorizer = _ => true;
                }
                context.Container.RegisterSingleton(authorizer);
                context.Pipeline.Register<SubscriptionReceiverBehavior.Registration>();
            }
        }
    }
}