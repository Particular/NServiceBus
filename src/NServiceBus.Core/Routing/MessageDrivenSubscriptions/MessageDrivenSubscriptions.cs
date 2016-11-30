namespace NServiceBus.Features
{
    using System;
    using Persistence;
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

            var canReceive = !context.Settings.GetOrDefault<bool>("Endpoint.SendOnly");
            var conventions = context.Settings.Get<Conventions>();
            var enforceBestPractices = context.Settings.Get<bool>(RoutingFeature.EnforceBestPracticesSettingsKey);

            var publishers = context.Settings.Get<Publishers>();
            var configuredPublishers = context.Settings.Get<ConfiguredPublishers>();

            configuredPublishers.Apply(publishers, conventions, enforceBestPractices);

            context.Pipeline.Register(b => new UnicastPublishRouterConnector(b.Build<MessageMetadataRegistry>(), b.Build<ISubscriptionStorage>()), "Determines how the published messages should be routed");

            if (canReceive)
            {
                var distributorAddress = context.Settings.GetOrDefault<string>("LegacyDistributor.Address");

                var subscriberAddress = distributorAddress ?? context.Settings.LocalAddress();

                context.Pipeline.Register(new MessageDrivenSubscribeConnector(publishers, subscriberAddress, context.Settings.EndpointName()), "Sends subscription requests when message driven subscriptions is in use");
                context.Pipeline.Register(new MessageDrivenSubscribeTerminator(), "Terminates subscribe pipeline");
                context.Pipeline.Register(new MessageDrivenUnsubscribeConnector(publishers, subscriberAddress, context.Settings.EndpointName()), "Sends requests to unsubscribe when message driven subscriptions is in use");
                context.Pipeline.Register(new MessageDrivenUnsubscribeTerminator(), "Termnates unsubscribe pipeline");

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