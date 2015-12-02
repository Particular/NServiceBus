namespace NServiceBus
{
    using System;
    using NServiceBus.Settings;
    using NServiceBus.Transports;

    /// <summary>
    /// Provides extensions for configuring message driven subscriptions.
    /// </summary>
    public static class MessageDrivenSubscriptionsConfigExtensions
    {
        /// <summary>
        /// Forces this endpoint to use a legacy message driven subscription mode which makes it compatible with V5 and previous versions.
        /// </summary>
        /// <remarks>
        /// When scaling out with queue per endpoint instance the legacy mode should be used only in a single instance. Enabling the legacy
        /// mode in multiple instances will result in message duplication.
        /// </remarks>
        public static void UseLegacyMessageDrivenSubscriptionMode(this BusConfiguration busConfiguration)
        {
            busConfiguration.Settings.Set("NServiceBus.Routing.UseLegacyMessageDrivenSubscriptionMode", true);
        }


        /// <summary>
        /// Set a <see cref="SubscriptionAuthorizer"/> to be used when verifying a <see cref="MessageIntentEnum.Subscribe"/> or <see cref="MessageIntentEnum.Unsubscribe"/> message.
        /// </summary>
        /// <remarks>This is a "single instance" extension point. So calling this api multiple time will result in only the last one added being executed at message receive time.</remarks>
        /// <param name="transportExtensions">The <see cref="TransportExtensions"/> to extend.</param>
        /// <param name="authorizer">The <see cref="SubscriptionAuthorizer"/> to execute.</param>
        public static void SubscriptionAuthorizer(this TransportExtensions transportExtensions, SubscriptionAuthorizer authorizer)
        {
            Guard.AgainstNull(nameof(authorizer), authorizer);
            var settings = transportExtensions.Settings;
            var transport = settings.Get<TransportDefinition>();
            if (transport.GetOutboundRoutingPolicy(settings).Publishes == OutboundRoutingType.Multicast)
            {
                var message = $"The transport {transport.GetType().Name} supports native publish-subscribe so subscriptions are not managed by the transport in the publishing endpoint. Use the native transport tools managing subscritions.";
                throw new ArgumentException(message, nameof(authorizer));
            }

            settings.Set<SubscriptionAuthorizer>(authorizer);
        }

        internal static SubscriptionAuthorizer GetSubscriptionAuthorizer(this ReadOnlySettings settings)
        {
            SubscriptionAuthorizer authorizer;
            settings.TryGet(out authorizer);
            return authorizer;
        }
    }
}