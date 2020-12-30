using System;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus.Transports;
using NServiceBus.Unicast.Messages;

namespace NServiceBus.Features
{
    using Transport;

    class NativePublishSubscribeFeature : Feature
    {
        public NativePublishSubscribeFeature()
        {
            EnableByDefault();
            Prerequisite(c => c.Settings.Get<TransportDefinition>().SupportsPublishSubscribe, "The transport does not support native pub sub");
            Prerequisite(c => SubscriptionMigrationMode.IsMigrationModeEnabled(c.Settings) == false, "The transport has enabled subscription migration mode");
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var canReceive = !context.Settings.GetOrDefault<bool>("Endpoint.SendOnly");

            context.Pipeline.Register("MulticastPublishRouterBehavior", new MulticastPublishConnector(), "Determines how the published messages should be routed");

            if (canReceive)
            {
                context.Pipeline.Register(b => new NativeSubscribeTerminator(b.GetRequiredService<ISubscriptionManager>(), b.GetRequiredService<MessageMetadataRegistry>()), "Requests the transport to subscribe to a given message type");
                context.Pipeline.Register(b => new NativeUnsubscribeTerminator(b.GetRequiredService<ISubscriptionManager>(), b.GetRequiredService<MessageMetadataRegistry>()), "Requests the transport to unsubscribe to a given message type");
            }
            else
            {
                context.Pipeline.Register(new SendOnlySubscribeTerminator(), "Throws an exception when trying to subscribe from a send-only endpoint");
                context.Pipeline.Register(new SendOnlyUnsubscribeTerminator(), "Throws an exception when trying to unsubscribe from a send-only endpoint");
            }
        }
    }
}