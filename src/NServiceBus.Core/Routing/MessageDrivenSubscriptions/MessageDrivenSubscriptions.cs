namespace NServiceBus.Features
{
    using System;
    using System.Threading.Tasks;
    using Persistence;
    using Pipeline;
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
            Prerequisite(c => c.Settings.GetOrDefault<IUnicastPublishProvider>() == null, $"{nameof(MessageDrivenSubscriptions)} are disabled because of the registered custom {nameof(IUnicastPublishProvider)}");
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

            context.Container.ConfigureComponent(b =>
            {
                var unicastPublishRouter = new UnicastPublishRouter(b.Build<MessageMetadataRegistry>(), b.Build<ISubscriptionStorage>());
                return new MessageDrivenPublishSubscribe(subscriptionRouter, subscriberAddress, context.Settings.EndpointName(), b.Build<IDispatchMessages>(), unicastPublishRouter, distributionPolicy);
            }, DependencyLifecycle.SingleInstance);

            context.Container.ConfigureComponent<IUnicastPublish>(b => b.Build<MessageDrivenPublishSubscribe>(), DependencyLifecycle.SingleInstance);
            
            if (canReceive)
            {
                context.Pipeline.Register(b => new PublishSubscribeTerminator(b.Build<MessageDrivenPublishSubscribe>()), "Handles subscribe requests for non-native publish subscribe.");
                context.Pipeline.Register(b => new PublishUnsubscribeTerminator(b.Build<MessageDrivenPublishSubscribe>()), "Handles unsubscribe requests for non-native publish subscribe.");
                
                var authorizer = context.Settings.GetSubscriptionAuthorizer();
                if (authorizer == null)
                {
                    authorizer = _ => true;
                }
                context.Container.RegisterSingleton(authorizer);
                context.Pipeline.Register<SubscriptionReceiverBehavior.Registration>();
            }
        }

        class PublishUnsubscribeTerminator : PipelineTerminator<IUnsubscribeContext>
        {
            MessageDrivenPublishSubscribe publishSubscribe;

            public PublishUnsubscribeTerminator(MessageDrivenPublishSubscribe publishSubscribe)
            {
                this.publishSubscribe = publishSubscribe;
            }

            protected override Task Terminate(IUnsubscribeContext context)
            {
                return publishSubscribe.Unsubscribe(context);
            }
        }

        class PublishSubscribeTerminator : PipelineTerminator<ISubscribeContext>
        {
            MessageDrivenPublishSubscribe publishSubscribe;

            public PublishSubscribeTerminator(MessageDrivenPublishSubscribe publishSubscribe)
            {
                this.publishSubscribe = publishSubscribe;
            }

            protected override Task Terminate(ISubscribeContext context)
            {
                return publishSubscribe.Subscribe(context);
            }
        }
    }
}