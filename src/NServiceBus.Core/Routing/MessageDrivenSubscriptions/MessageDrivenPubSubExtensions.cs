namespace NServiceBus
{
    using System;
    using System.Reflection;
    using Routing;
    using Routing.MessageDrivenSubscriptions;
    using Settings;
    using Transports;

    /// <summary>
    /// Extensions for configuring message-driven pub sub.
    /// </summary>
    public static class MessageDrivenPubSubExtensions
    {
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

        static T GetOrCreate<T>(this SettingsHolder settings)
            where T : new()
        {
            T value;
            if (!settings.TryGet(out value))
            {
                value = new T();
                settings.Set<T>(value);
            }
            return value;
        }
    }
}