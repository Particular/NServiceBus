namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Config;
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
                s.SetDefault<ConfiguredPublishers>(new ConfiguredPublishers());
                s.SetDefault<Publishers>(new Publishers());

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
            var conventions = context.Settings.Get<Conventions>();
            var enforceBestPractices = context.Settings.Get<bool>(RoutingFeature.EnforceBestPracticesSettingsKey);
            var unicastBusConfig = context.Settings.GetConfigSection<UnicastBusConfig>();
            var distributorAddress = context.Settings.GetOrDefault<string>("LegacyDistributor.Address");

            var routing = context.Settings.Get<IRoutingComponent>();
            var publishers = context.Settings.Get<Publishers>();
            var configuredPublishers = context.Settings.Get<ConfiguredPublishers>();

            ApplyLegacyConfiguration(unicastBusConfig?.MessageEndpointMappings, publishers, transportInfrastructure.MakeCanonicalForm, conventions);
            configuredPublishers.Apply(publishers, conventions, enforceBestPractices);

            context.Pipeline.Register(b => new RefreshSubscribersBehavior(b.Build<ISubscriptionStorage>(), routing.Publishing, b.Build<MessageMetadataRegistry>(), TimeSpan.FromSeconds(0)),
                "Forces refreshing of subscriber table based on the subscription storage");

            var subscriberAddress = distributorAddress ?? context.Settings.LocalAddress();
            var subscriptionRouter = new SubscriptionRouter(publishers, routing.EndpointInstances, i => transportInfrastructure.ToTransportAddress(LogicalAddress.CreateRemoteAddress(i)));

            routing.RegisterSubscriptionHandler(b => new MessageDrivenSubscriptionManager(subscriptionRouter, subscriberAddress, context.Settings.EndpointName(), b.Build<IDispatchMessages>()));

            var authorizer = context.Settings.GetSubscriptionAuthorizer();
            if (authorizer == null)
            {
                authorizer = _ => true;
            }
            context.Container.RegisterSingleton(authorizer);
            context.Pipeline.Register<SubscriptionReceiverBehavior.Registration>();
        }

        static void ApplyLegacyConfiguration(MessageEndpointMappingCollection mappings, Publishers publishers, Func<string, string> makeCanonicalAddress, Conventions conventions)
        {
            if (mappings == null)
            {
                return;
            }
            var publisherTableEntries = new List<PublisherTableEntry>();

            mappings.Apply(makeCanonicalAddress, (type, address, baseTypes) =>
            {
                var publisherAddress = PublisherAddress.CreateFromPhysicalAddresses(address);
                publisherTableEntries.AddRange(baseTypes.Select(type1 => new PublisherTableEntry(type1, publisherAddress)));
                publisherTableEntries.Add(new PublisherTableEntry(type, publisherAddress));
            }, conventions);

            publishers.AddOrReplacePublishers("MessageEndpointMappings", publisherTableEntries);
        }
    }
}