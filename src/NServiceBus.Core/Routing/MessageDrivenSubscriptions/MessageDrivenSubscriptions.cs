using NServiceBus.Transports;

namespace NServiceBus.Features
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Transport;
    using Unicast.Messages;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    /// <summary>
    /// Allows subscribers to register by sending a subscription message to this endpoint.
    /// ---
    /// The goal is to remove feature classes that implemented functionality far beyond what features are "supposed" to be.
    /// Many of those features have been moved into components instead. Now that this class is internal in V8 that
    /// refactoring can occur.
    /// </summary>
    [ObsoleteEx(
        Message = "It's not recommended to disable the MessageDrivenSubscriptions feature and this option will be removed in future versions. Use 'TransportExtensions<T>.DisablePublishing()' to avoid the need for a subscription storage if this endpoint does not publish events.",
        RemoveInVersion = "10",
        TreatAsErrorFromVersion = "9")]
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
            Prerequisite(c => c.Settings.Get<TransportDefinition>().SupportsPublishSubscribe == false || SubscriptionMigrationMode.IsMigrationModeEnabled(c.Settings), "The transport supports native pub sub");
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
            var transportDefinition = context.Settings.Get<TransportDefinition>();
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

                context.Pipeline.Register("UnicastPublishRouterConnector", b =>
                {
                    var unicastPublishRouter = new UnicastPublishRouter(b.GetRequiredService<MessageMetadataRegistry>(), i => transportDefinition.ToTransportAddress(new QueueAddress(i.Endpoint, i.Discriminator, i.Properties, null)), b.GetRequiredService<ISubscriptionStorage>());
                    return new UnicastPublishConnector(unicastPublishRouter, distributionPolicy);
                }, "Determines how the published messages should be routed");

                var authorizer = context.Settings.GetSubscriptionAuthorizer();
                if (authorizer == null)
                {
                    authorizer = _ => true;
                }
                context.Container.AddSingleton(authorizer);
                context.Pipeline.Register(typeof(SubscriptionReceiverBehavior), "Check for subscription messages and execute the requested behavior to subscribe or unsubscribe.") ;
            }
            else
            {
                context.Pipeline.Register(typeof(DisabledPublishingTerminator), "Throws an exception when trying to publish with publishing disabled");
            }

            var canReceive = !context.Settings.GetOrDefault<bool>("Endpoint.SendOnly");
            if (canReceive)
            {
                var subscriberAddress = context.Receiving.LocalAddress;
                var subscriptionRouter = new SubscriptionRouter(publishers, endpointInstances, i => transportDefinition.ToTransportAddress(new QueueAddress(i.Endpoint, i.Discriminator, i.Properties, null)));

                context.Pipeline.Register(b => new MessageDrivenSubscribeTerminator(subscriptionRouter, subscriberAddress, context.Settings.EndpointName(), b.GetRequiredService<IMessageDispatcher>()), "Sends subscription requests when message driven subscriptions is in use");
                context.Pipeline.Register(b => new MessageDrivenUnsubscribeTerminator(subscriptionRouter, subscriberAddress, context.Settings.EndpointName(), b.GetRequiredService<IMessageDispatcher>()), "Sends requests to unsubscribe when message driven subscriptions is in use");
            }
            else
            {
                context.Pipeline.Register(new SendOnlySubscribeTerminator(), "Throws an exception when trying to subscribe from a send-only endpoint");
                context.Pipeline.Register(new SendOnlyUnsubscribeTerminator(), "Throws an exception when trying to unsubscribe from a send-only endpoint");
            }

            // implementations of IInitializableSubscriptionStorage are optional and can be provided by persisters.
            context.RegisterStartupTask(b => new InitializableSubscriptionStorage(b.GetService<IInitializableSubscriptionStorage>()));
        }

        internal class InitializableSubscriptionStorage : FeatureStartupTask
        {
            IInitializableSubscriptionStorage subscriptionStorage;

            public InitializableSubscriptionStorage(IInitializableSubscriptionStorage subscriptionStorage)
            {
                this.subscriptionStorage = subscriptionStorage;
            }

            protected override Task OnStart(IMessageSession session)
            {
                subscriptionStorage?.Init();
                return Task.CompletedTask;
            }

            protected override Task OnStop(IMessageSession session)
            {
                return Task.CompletedTask;
            }
        }
    }
}