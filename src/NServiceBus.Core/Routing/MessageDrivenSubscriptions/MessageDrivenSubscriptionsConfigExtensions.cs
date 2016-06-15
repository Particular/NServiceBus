namespace NServiceBus
{
    using System;
    using System.Reflection;
    using Pipeline;
    using Routing;
    using Routing.MessageDrivenSubscriptions;
    using Settings;
    using Transports;

    /// <summary>
    /// Provides extensions for configuring message driven subscriptions.
    /// </summary>
    public static class MessageDrivenSubscriptionsConfigExtensions
    {
        /// <summary>
        /// Set a Authorizer to be used when verifying a <see cref="MessageIntentEnum.Subscribe" /> or
        /// <see cref="MessageIntentEnum.Unsubscribe" /> message.
        /// </summary>
        /// <remarks>
        /// This is a "single instance" extension point. So calling this api multiple time will result in only the last
        /// one added being executed at message receive time.
        /// </remarks>
        /// <param name="transportExtensions">The <see cref="TransportExtensions" /> to extend.</param>
        /// <param name="authorizer">The <see cref="Func{TI,TR}" /> to execute.</param>
        public static void SubscriptionAuthorizer<T>(this TransportExtensions<T> transportExtensions, Func<IIncomingPhysicalMessageContext, bool> authorizer) where T : TransportDefinition, IMessageDrivenSubscriptionTransport
        {
            Guard.AgainstNull(nameof(authorizer), authorizer);
            var settings = transportExtensions.Settings;

            settings.Set("SubscriptionAuthorizer", authorizer);
        }

        internal static Func<IIncomingPhysicalMessageContext, bool> GetSubscriptionAuthorizer(this ReadOnlySettings settings)
        {
            Func<IIncomingPhysicalMessageContext, bool> authorizer;
            settings.TryGet("SubscriptionAuthorizer", out authorizer);
            return authorizer;
        }

        /// <summary>
        /// Registers a publisherEndpoint endpoint for a given endpoint type.
        /// </summary>
        /// <param name="extensions">Extensions object.</param>
        /// <param name="publisherEndpoint">Publisher endpoint.</param>
        /// <param name="eventType">Event type.</param>
        public static void RegisterPublisherForType<T>(this TransportExtensions<T> extensions, string publisherEndpoint, Type eventType) where T : TransportDefinition, IMessageDrivenSubscriptionTransport
        {
            extensions.Settings.GetOrCreate<Publishers>().Add(publisherEndpoint, eventType);
        }

        /// <summary>
        /// Registers a publisherEndpoint for all events in a given assembly (and optionally namespace).
        /// </summary>
        /// <param name="extensions">Extensions.</param>
        /// <param name="publisherEndpoint">Publisher endpoint.</param>
        /// <param name="eventAssembly">Assembly containing events.</param>
        /// <param name="eventNamespace">Optional namespace containing events.</param>
        public static void RegisterPublisherForAssembly<T>(this TransportExtensions<T> extensions, string publisherEndpoint, Assembly eventAssembly, string eventNamespace = null) where T : TransportDefinition, IMessageDrivenSubscriptionTransport
        {
            extensions.Settings.GetOrCreate<Publishers>().Add(publisherEndpoint, eventAssembly, eventNamespace);
        }
    }
}