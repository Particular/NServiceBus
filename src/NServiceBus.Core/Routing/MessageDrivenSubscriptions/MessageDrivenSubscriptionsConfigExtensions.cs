namespace NServiceBus
{
    using System;
    using System.Reflection;
    using Features;
    using Pipeline;
    using Routing;
    using Routing.MessageDrivenSubscriptions;
    using Settings;
    using Transport;

    /// <summary>
    /// Provides extensions for configuring message driven subscriptions.
    /// </summary>
    public static partial class MessageDrivenSubscriptionsConfigExtensions
    {
        /// <summary>
        /// Sets an authorizer to be used when processing a <see cref="MessageIntent.Subscribe" /> or
        /// <see cref="MessageIntent.Unsubscribe" /> message.
        /// </summary>
        /// <param name="routingSettings">The <see cref="RoutingSettings&lt;T&gt;" /> to extend.</param>
        /// <param name="authorizer">The authorization callback to execute. If the callback returns <code>true</code> for a message, it is authorized to subscribe/unsubscribe, otherwise it is not authorized.</param>
        public static void SubscriptionAuthorizer<T>(this RoutingSettings<T> routingSettings, Func<IIncomingPhysicalMessageContext, bool> authorizer) where T : TransportDefinition, IMessageDrivenSubscriptionTransport
        {
            Guard.AgainstNull(nameof(authorizer), authorizer);
            var settings = routingSettings.Settings;

            settings.Set("SubscriptionAuthorizer", authorizer);
        }

        internal static Func<IIncomingPhysicalMessageContext, bool> GetSubscriptionAuthorizer(this IReadOnlySettings settings)
        {
            settings.TryGet("SubscriptionAuthorizer", out Func<IIncomingPhysicalMessageContext, bool> authorizer);
            return authorizer;
        }

        /// <summary>
        /// Disables the ability to publish events. This removes the need to provide a subscription storage option. The endpoint can still subscribe to events but isn't allowed to publish its events.
        /// </summary>
        public static void DisablePublishing<T>(this RoutingSettings<T> routingSettings) where T : TransportDefinition, IMessageDrivenSubscriptionTransport
        {
            routingSettings.Settings.Set(MessageDrivenSubscriptions.EnablePublishingSettingsKey, false);
        }

        /// <summary>
        /// Registers a publisher endpoint for a given event type.
        /// </summary>
        /// <param name="routingSettings">The <see cref="RoutingSettings&lt;T&gt;" /> to extend.</param>
        /// <param name="eventType">The event type.</param>
        /// <param name="publisherEndpoint">The publisher endpoint.</param>
        public static void RegisterPublisher<T>(this RoutingSettings<T> routingSettings, Type eventType, string publisherEndpoint) where T : TransportDefinition, IMessageDrivenSubscriptionTransport
        {
            Guard.AgainstNullAndEmpty(nameof(publisherEndpoint), publisherEndpoint);

            ThrowOnAddress(publisherEndpoint);
            routingSettings.Settings.GetOrCreate<ConfiguredPublishers>().Add(new TypePublisherSource(eventType, PublisherAddress.CreateFromEndpointName(publisherEndpoint)));
        }

        /// <summary>
        /// Registers a publisher endpoint for all event types in a given assembly.
        /// </summary>
        /// <param name="routingSettings">The <see cref="RoutingSettings&lt;T&gt;" /> to extend.</param>
        /// <param name="assembly">The assembly containing the event types.</param>
        /// <param name="publisherEndpoint">The publisher endpoint.</param>
        public static void RegisterPublisher<T>(this RoutingSettings<T> routingSettings, Assembly assembly, string publisherEndpoint) where T : TransportDefinition, IMessageDrivenSubscriptionTransport
        {
            Guard.AgainstNull(nameof(assembly), assembly);
            Guard.AgainstNullAndEmpty(nameof(publisherEndpoint), publisherEndpoint);

            ThrowOnAddress(publisherEndpoint);

            routingSettings.Settings.GetOrCreate<ConfiguredPublishers>().Add(new AssemblyPublisherSource(assembly, PublisherAddress.CreateFromEndpointName(publisherEndpoint)));
        }

        /// <summary>
        /// Registers a publisher endpoint for all event types in a given assembly and namespace.
        /// </summary>
        /// <param name="routingSettings">The <see cref="RoutingSettings&lt;T&gt;" /> to extend.</param>
        /// <param name="assembly">The assembly containing the event types.</param>
        /// <param name="namespace"> The namespace containing the event types. The given value must exactly match the target namespace.</param>
        /// <param name="publisherEndpoint">The publisher endpoint.</param>
        public static void RegisterPublisher<T>(this RoutingSettings<T> routingSettings, Assembly assembly, string @namespace, string publisherEndpoint) where T : TransportDefinition, IMessageDrivenSubscriptionTransport
        {
            Guard.AgainstNull(nameof(assembly), assembly);
            Guard.AgainstNullAndEmpty(nameof(publisherEndpoint), publisherEndpoint);

            ThrowOnAddress(publisherEndpoint);

            // empty namespace is null, not string.empty
            @namespace = @namespace == string.Empty ? null : @namespace;

            routingSettings.Settings.GetOrCreate<ConfiguredPublishers>().Add(new NamespacePublisherSource(assembly, @namespace, PublisherAddress.CreateFromEndpointName(publisherEndpoint)));
        }

        static void ThrowOnAddress(string publisherEndpoint)
        {
            if (publisherEndpoint.Contains("@"))
            {
                throw new ArgumentException($"A logical endpoint name should not contain '@', but received '{publisherEndpoint}'. To specify an endpoint's address, use the instance mapping file for the MSMQ transport, or refer to the routing documentation.");
            }
        }
    }
}
