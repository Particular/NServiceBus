namespace NServiceBus.Features
{
    using System;
    using Transport;
    using Unicast.Messages;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    /// <summary>
    /// Allows subscribers to register by sending a subscription message to this endpoint.
    /// </summary>
    [ObsoleteEx(Message = "It's not recommended to disable the MessageDrivenSubscriptions feature and this option will be removed in future versions. Use 'TransportExtensions<T>.DisablePublishing()' to avoid the need for a subscription storage if this endpoint does not publish events.",
        RemoveInVersion = "8.0",
        TreatAsErrorFromVersion = "8.0")]
    public class MessageDrivenSubscriptions : Feature
    {
        internal const string EnablePublishingSettingsKey = "NServiceBus.PublishSubscribe.EnablePublishing";

        internal MessageDrivenSubscriptions()
        {
            EnableByDefault();
            Defaults(s =>
            {
                // s.SetDefault<Publishers>(new Publishers()); currently setup by RoutingFeature
                s.SetDefault(new ConfiguredPublishers());
                s.SetDefault(EnablePublishingSettingsKey, true);
            });
            Prerequisite(c => c.Settings.Get<TransportInfrastructure>().OutboundRoutingPolicy.Publishes == OutboundRoutingType.Unicast || SubscriptionMigrationMode.IsMigrationModeEnabled(c.Settings), "The transport supports native pub sub");
        }

        /// <summary>
        /// See <see cref="Feature.Setup" />.
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            // The MessageDrivenSubscriptions feature needs to be activated when using the subscription migration mode as some persister packages check this feature before enabling the subscription storage.
            if (SubscriptionMigrationMode.IsMigrationModeEnabled(context.Settings))
            {
                return;
            }

            var transportInfrastructure = context.Settings.Get<TransportInfrastructure>();
            var conventions = context.Settings.Get<Conventions>();
            var enforceBestPractices = context.Routing.EnforceBestPractices;

            var distributionPolicy = context.Routing.DistributionPolicy;
            var endpointInstances = context.Routing.EndpointInstances;
            var publishers = context.Routing.Publishers;

            var configuredPublishers = context.Settings.Get<ConfiguredPublishers>();
            configuredPublishers.Apply(publishers, conventions, enforceBestPractices);

            var publishingEnabled = context.Settings.Get<bool>(EnablePublishingSettingsKey);
            if (publishingEnabled)
            {
                if (!PersistenceStartup.HasSupportFor<StorageType.Subscriptions>(context.Settings))
                {
                    throw new Exception("The selected persistence doesn't have support for subscription storage. Select another persistence or disable the publish functionality using transportConfiguration.DisablePublishing()");
                }

                context.Pipeline.Register(b =>
                {
                    var unicastPublishRouter = new UnicastPublishRouter(b.Build<MessageMetadataRegistry>(), i => transportInfrastructure.ToTransportAddress(LogicalAddress.CreateRemoteAddress(i)), b.Build<ISubscriptionStorage>());
                    return new UnicastPublishConnector(unicastPublishRouter, distributionPolicy);
                }, "Determines how the published messages should be routed");
            }
            else
            {
                context.Pipeline.Register(typeof(DisabledPublishingTerminator), "Throws an exception when trying to publish with publishing disabled");
            }

            var canReceive = !context.Settings.GetOrDefault<bool>("Endpoint.SendOnly");
            if (canReceive)
            {
                var subscriberAddress = context.Receiving.LocalAddress;
                var subscriptionRouter = new SubscriptionRouter(publishers, endpointInstances, i => transportInfrastructure.ToTransportAddress(LogicalAddress.CreateRemoteAddress(i)));

                context.Pipeline.Register(b => new MessageDrivenSubscribeTerminator(subscriptionRouter, subscriberAddress, context.Settings.EndpointName(), b.Build<IDispatchMessages>()), "Sends subscription requests when message driven subscriptions is in use");
                context.Pipeline.Register(b => new MessageDrivenUnsubscribeTerminator(subscriptionRouter, subscriberAddress, context.Settings.EndpointName(), b.Build<IDispatchMessages>()), "Sends requests to unsubscribe when message driven subscriptions is in use");

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