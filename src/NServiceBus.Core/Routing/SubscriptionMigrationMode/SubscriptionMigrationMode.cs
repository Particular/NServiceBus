using NServiceBus.Transports;

namespace NServiceBus.Features
{
    using Microsoft.Extensions.DependencyInjection;
    using Settings;
    using Transport;
    using Unicast.Messages;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    class SubscriptionMigrationMode : Feature
    {
        public SubscriptionMigrationMode()
        {
            EnableByDefault();
            Prerequisite(c => c.Settings.Get<TransportDefinition>().SupportsPublishSubscribe, "The transport does not support native pub sub");
            Prerequisite(c => IsMigrationModeEnabled(c.Settings), "The transport has not enabled subscription migration mode");
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var transportInfrastructure = context.Settings.Get<TransportInfrastructure>();
            var transportDefinition = context.Settings.Get<TransportDefinition>();
            var canReceive = !context.Settings.GetOrDefault<bool>("Endpoint.SendOnly");

            var distributionPolicy = context.Routing.DistributionPolicy;
            var publishers = context.Routing.Publishers;
            var configuredPublishers = context.Settings.Get<ConfiguredPublishers>();
            var conventions = context.Settings.Get<Conventions>();
            var enforceBestPractices = context.Routing.EnforceBestPractices;

            configuredPublishers.Apply(publishers, conventions, enforceBestPractices);

            context.Pipeline.Register(b =>
            {
                var unicastPublishRouter = new UnicastPublishRouter(b.GetRequiredService<MessageMetadataRegistry>(), i =>
                {
                    var queueAddress = new QueueAddress(i.Endpoint, i.Discriminator, i.Properties, null);
                    return transportDefinition.ToTransportAddress(queueAddress);
                }, b.GetRequiredService<ISubscriptionStorage>());
                return new MigrationModePublishConnector(distributionPolicy, unicastPublishRouter);
            }, "Determines how the published messages should be routed");

            if (canReceive)
            {
                var endpointInstances = context.Routing.EndpointInstances;
                var mainReceiver = context.Settings.Get<TransportInfrastructure>().GetReceiver(ReceiveComponent.MainReceiverId);
                var subscriptionManager = mainReceiver.Subscriptions;

                var subscriptionRouter = new SubscriptionRouter(publishers, endpointInstances, i =>
                {
                    var queueAddress = new QueueAddress(i.Endpoint, i.Discriminator, i.Properties, null);
                    return transportDefinition.ToTransportAddress(queueAddress);
                });
                var subscriberAddress = context.Receiving.LocalAddress;

                context.Pipeline.Register(b =>
                    new MigrationSubscribeTerminator(subscriptionManager, b.GetRequiredService<MessageMetadataRegistry>(), subscriptionRouter, b.GetRequiredService<IMessageDispatcher>(), subscriberAddress, context.Settings.EndpointName()), "Requests the transport to subscribe to a given message type");
                context.Pipeline.Register(b =>
                    new MigrationUnsubscribeTerminator(subscriptionManager, b.GetRequiredService<MessageMetadataRegistry>(), subscriptionRouter, b.GetRequiredService<IMessageDispatcher>(), subscriberAddress, context.Settings.EndpointName()), "Sends requests to unsubscribe when message driven subscriptions is in use");

                var authorizer = context.Settings.GetSubscriptionAuthorizer();
                if (authorizer == null)
                {
                    authorizer = _ => true;
                }
                context.Container.AddSingleton(authorizer);
                context.Pipeline.Register(typeof(SubscriptionReceiverBehavior), "Check for subscription messages and execute the requested behavior to subscribe or unsubscribe.");
            }
            else
            {
                context.Pipeline.Register(new SendOnlySubscribeTerminator(), "Throws an exception when trying to subscribe from a send-only endpoint");
                context.Pipeline.Register(new SendOnlyUnsubscribeTerminator(), "Throws an exception when trying to unsubscribe from a send-only endpoint");
            }

            // implementations of IInitializableSubscriptionStorage are optional and can be provided by persisters.
            context.RegisterStartupTask(b => new MessageDrivenSubscriptions.InitializableSubscriptionStorage(b.GetService<IInitializableSubscriptionStorage>()));
        }

        public static bool IsMigrationModeEnabled(ReadOnlySettings settings)
        {
            // this key can be set by transports once they provide native support for pub/sub.
            return settings.TryGet("NServiceBus.Subscriptions.EnableMigrationMode", out bool enabled) && enabled;
        }
    }
}