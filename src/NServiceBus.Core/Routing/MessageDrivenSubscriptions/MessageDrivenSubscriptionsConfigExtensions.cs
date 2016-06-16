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
        /// Sets an authorizer to be used when verifying a <see cref="MessageIntentEnum.Subscribe" /> or
        /// <see cref="MessageIntentEnum.Unsubscribe" /> message.
        /// </summary>
        /// <remarks>
        /// This is a "single instance" extension point, so calling this api multiple times will result in only the last
        /// one added being executed at message receive time.
        /// </remarks>
        /// <param name="transportExtensions">The <see cref="TransportExtensions&lt;T&gt;" /> to extend.</param>
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
        /// Registers a publisher endpoint for a given event type.
        /// </summary>
        /// <param name="transportExtensions">The <see cref="TransportExtensions&lt;T&gt;" /> to extend.</param>
        /// <param name="publisherEndpoint">The publisher endpoint.</param>
        /// <param name="eventType">The event type.</param>
        public static void RegisterPublisherForType<T>(this TransportExtensions<T> transportExtensions, string publisherEndpoint, Type eventType) where T : TransportDefinition, IMessageDrivenSubscriptionTransport
        {
            transportExtensions.Settings.GetOrCreate<Publishers>().Add(publisherEndpoint, eventType);
        }

        /// <summary>
        /// Registers a publisher endpoint for all event types in a given assembly and, optionally, namespace.
        /// </summary>
        /// <param name="transportExtensions">The <see cref="TransportExtensions&lt;T&gt;" /> to extend.</param>
        /// <param name="publisherEndpoint">The publisher endpoint.</param>
        /// <param name="eventAssembly">The assembly containing the event types.</param>
        /// <param name="eventNamespace">The namespace containing the event types.</param>
        public static void RegisterPublisherForAssembly<T>(this TransportExtensions<T> transportExtensions, string publisherEndpoint, Assembly eventAssembly, string eventNamespace = null) where T : TransportDefinition, IMessageDrivenSubscriptionTransport
        {
            transportExtensions.Settings.GetOrCreate<Publishers>().Add(publisherEndpoint, eventAssembly, eventNamespace);
        }
    }
}