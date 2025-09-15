namespace NServiceBus.Features;

using System;
using Microsoft.Extensions.DependencyInjection;
using Transport;
using Unicast.Messages;
using Unicast.Subscriptions.MessageDrivenSubscriptions;

/// <summary>
/// Allows subscribers to register by sending a subscription message to this endpoint.
/// </summary>

// The goal is to remove feature classes that implement functionality far beyond what features are "supposed" to be.
// Many of those features have been moved into components instead.
// Now that NSB 10 has made the class internal, refactoring can occur.
class MessageDrivenSubscriptions : Feature
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

        if (ActivitySources.Main.HasListeners())
        {
            context.Pipeline.Register(new SubscribeDiagnosticsBehavior(), "Adds additional subscribe diagnostic attributes to OpenTelemetry spans");
            context.Pipeline.Register(new UnsubscribeDiagnosticsBehavior(), "Adds additional unsubscribe diagnostic attributes to OpenTelemetry spans");
        }

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
            if (!PersistenceComponent.HasSupportFor<StorageType.Subscriptions>(context.Settings))
            {
                throw new Exception("The selected persistence doesn't have support for subscription storage. Select another persistence or disable the publish functionality using transportConfiguration.DisablePublishing()");
            }

            context.Pipeline.Register("UnicastPublishRouterConnector", b =>
            {
                var unicastPublishRouter = new UnicastPublishRouter(
                    b.GetRequiredService<MessageMetadataRegistry>(),
                    b.GetRequiredService<ITransportAddressResolver>(),
                    b.GetRequiredService<ISubscriptionStorage>());
                return new UnicastPublishConnector(unicastPublishRouter, distributionPolicy);
            }, "Determines how the published messages should be routed");

            var authorizer = context.Settings.GetSubscriptionAuthorizer();
            authorizer ??= _ => true;
            context.Services.AddSingleton(authorizer);
            context.Pipeline.Register(typeof(SubscriptionReceiverBehavior), "Check for subscription messages and execute the requested behavior to subscribe or unsubscribe.");
        }
        else
        {
            context.Pipeline.Register(typeof(DisabledPublishingTerminator), "Throws an exception when trying to publish with publishing disabled");
        }

        var canReceive = !context.Settings.GetOrDefault<bool>("Endpoint.SendOnly");
        if (canReceive)
        {
            context.Services.AddSingleton(b =>
            {
                var transportAddressResolver = b.GetRequiredService<ITransportAddressResolver>();
                return new SubscriptionRouter(
                    publishers,
                    endpointInstances,
                    i => transportAddressResolver.ToTransportAddress(
                        new QueueAddress(i.Endpoint, i.Discriminator, i.Properties)));
            });

            context.Pipeline.Register(b =>
                    new MessageDrivenSubscribeTerminator(
                        b.GetRequiredService<SubscriptionRouter>(),
                        b.GetRequiredService<ReceiveAddresses>(),
                        context.Settings.EndpointName(),
                        b.GetRequiredService<IMessageDispatcher>()),
                "Sends subscription requests when message driven subscriptions is in use");
            context.Pipeline.Register(b =>
                new MessageDrivenUnsubscribeTerminator(
                    b.GetRequiredService<SubscriptionRouter>(),
                    b.GetRequiredService<ReceiveAddresses>(),
                    context.Settings.EndpointName(),
                    b.GetRequiredService<IMessageDispatcher>()),
                "Sends requests to unsubscribe when message driven subscriptions is in use");
        }
        else
        {
            context.Pipeline.Register(new SendOnlySubscribeTerminator(), "Throws an exception when trying to subscribe from a send-only endpoint");
            context.Pipeline.Register(new SendOnlyUnsubscribeTerminator(), "Throws an exception when trying to unsubscribe from a send-only endpoint");
        }
    }
}