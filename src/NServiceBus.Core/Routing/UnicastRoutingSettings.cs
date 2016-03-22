namespace NServiceBus
{
    using System;
    using System.Reflection;
    using Configuration.AdvanceExtensibility;
    using Routing;
    using Routing.MessageDrivenSubscriptions;
    using Settings;

    /// <summary>
    /// Exposes settings related to routing.
    /// </summary>
    public class UnicastRoutingSettings : ExposeSettings
    {
        internal UnicastRoutingSettings(SettingsHolder settings)
            : base(settings)
        {
            Mapping = new RoutingMappingSettings(settings);
        }

        /// <summary>
        /// Adds a static unicast route.
        /// </summary>
        /// <param name="messageType">Message type.</param>
        /// <param name="destination">Destination endpoint.</param>
        public void RouteToEndpoint(Type messageType, string destination)
        {
            GetOrCreate<UnicastRoutingTable>().RouteToEndpoint(messageType, destination);
        }

        /// <summary>
        /// Registers a publisherEndpoint endpoint for a given endpoint type.
        /// </summary>
        /// <param name="publisherEndpoint">Publisher endpoint.</param>
        /// <param name="eventType">Event type.</param>
        public void AddPublisher(string publisherEndpoint, Type eventType)
        {
            GetOrCreate<Publishers>().Add(publisherEndpoint, eventType);
        }

        /// <summary>
        /// Registers a publisherEndpoint for all events in a given assembly (and optionally namespace).
        /// </summary>
        /// <param name="publisherEndpoint">Publisher endpoint.</param>
        /// <param name="eventAssembly">Assembly containing events.</param>
        /// <param name="eventNamespace">Optional namespace containing events.</param>
        public void AddPublisher(string publisherEndpoint, Assembly eventAssembly, string eventNamespace = null)
        {
            GetOrCreate<Publishers>().Add(publisherEndpoint, eventAssembly, eventNamespace);
        }


        /// <summary>
        /// Allows customizing advanced routing settings.
        /// </summary>
        public RoutingMappingSettings Mapping { get; }

        T GetOrCreate<T>()
            where T : new()
        {
            T value;
            if (!Settings.TryGet(out value))
            {
                value = new T();
                Settings.Set<T>(value);
            }
            return value;
        }
    }
}