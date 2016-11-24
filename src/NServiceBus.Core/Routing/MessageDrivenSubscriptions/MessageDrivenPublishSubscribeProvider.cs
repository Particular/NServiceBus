namespace NServiceBus.Features
{
    using System;
    using ObjectBuilder;
    using Persistence;
    using Transport;
    using Unicast.Messages;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    class MessageDrivenPublishSubscribeProvider : IPublishSubscribeProvider
    {
        public Func<IBuilder, IPublishRouter> GetRouter(FeatureConfigurationContext context)
        {
            var distributionPolicy = context.Routing.DistributionPolicy;

            ConfiguredPublishers configuredPublishers;
            if (context.Settings.TryGet(out configuredPublishers))
            {
                var conventions = context.Settings.Get<Conventions>();

                configuredPublishers.Apply(context.Routing.Publishers, conventions, context.Routing.EnforceBestPractices);
            }

            return builder => new UnicastPublishRouter(
                builder.Build<MessageMetadataRegistry>(),
                builder.Build<ISubscriptionStorage>(),
                distributionPolicy);
        }

        public Func<IBuilder, IManageSubscriptions> GetSubscriptionManager(FeatureConfigurationContext context)
        {
            if (!PersistenceStartup.HasSupportFor<StorageType.Subscriptions>(context.Settings))
            {
                throw new Exception("The selected persistence doesn't have support for subscription storage. Select another persistence or disable the message-driven subscriptions feature using endpointConfiguration.DisableFeature<MessageDrivenSubscriptions>()");
            }

            var transportInfrastructure = context.Settings.Get<TransportInfrastructure>();
            var subscriptionRouter = new SubscriptionRouter(context.Routing.Publishers, context.Routing.EndpointInstances, i => transportInfrastructure.ToTransportAddress(LogicalAddress.CreateRemoteAddress(i)));
            var distributorAddress = context.Settings.GetOrDefault<string>("LegacyDistributor.Address");
            var subscriberAddress = distributorAddress ?? context.Settings.LocalAddress();

            var authorizer = context.Settings.GetSubscriptionAuthorizer();
            if (authorizer == null)
            {
                authorizer = _ => true;
            }
            context.Container.RegisterSingleton(authorizer);
            context.Pipeline.Register<SubscriptionReceiverBehavior.Registration>();

            return builder => new MessageDrivenSubscriptionManager(subscriberAddress, context.Settings.EndpointName(), builder.Build<IDispatchMessages>(), subscriptionRouter);
        }
    }
}