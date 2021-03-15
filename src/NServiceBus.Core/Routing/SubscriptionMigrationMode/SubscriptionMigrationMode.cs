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
                var unicastPublishRouter = new UnicastPublishRouter(
                    b.GetRequiredService<MessageMetadataRegistry>(),
                    b.GetRequiredService<ISubscriptionStorage>(),
                    b.GetRequiredService<TransportInfrastructure>());
                return new MigrationModePublishConnector(distributionPolicy, unicastPublishRouter);
            }, "Determines how the published messages should be routed");

            if (canReceive)
            {
                var endpointInstances = context.Routing.EndpointInstances;

                //TODO bubble up usage of TI to method (and let terminators use it, as they need to translate the subscriber address anyway
                context.Container.AddSingleton(sp => new SubscriptionRouter(publishers, endpointInstances,
                    sp.GetRequiredService<TransportInfrastructure>()));

                var subscriberAddress = context.Receiving.LocalAddress;

                //TODO translate when resolving
                context.Pipeline.Register(b =>
                    new MigrationSubscribeTerminator(b.GetRequiredService<ISubscriptionManager>(), b.GetRequiredService<MessageMetadataRegistry>(),
                        b.GetRequiredService<SubscriptionRouter>(),
                        b.GetRequiredService<IMessageDispatcher>(),
                        "subscriberAddress",
                        context.Settings.EndpointName()), "Requests the transport to subscribe to a given message type");
                //TODO translate when resolving
                context.Pipeline.Register(b =>
                    new MigrationUnsubscribeTerminator(b.GetRequiredService<ISubscriptionManager>(), b.GetRequiredService<MessageMetadataRegistry>(),
                        b.GetRequiredService<SubscriptionRouter>(),
                        b.GetRequiredService<IMessageDispatcher>(),
                        "subscriberAddress",
                        context.Settings.EndpointName()), "Sends requests to unsubscribe when message driven subscriptions is in use");

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