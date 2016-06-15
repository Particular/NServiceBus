namespace NServiceBus
{
    using System;
    using Configuration.AdvanceExtensibility;
    using Routing;
    using Settings;

    /// <summary>
    /// Exposes settings related to routing.
    /// </summary>
    public class RoutingSettings : ExposeSettings
    {
        internal RoutingSettings(SettingsHolder settings)
            : base(settings)
        {
            Mapping = new RoutingMappingSettings(settings);
        }

        /// <summary>
        /// Allows customizing advanced routing settings.
        /// </summary>
        public RoutingMappingSettings Mapping { get; }

        /// <summary>
        /// Adds a static unicast route.
        /// </summary>
        /// <param name="messageType">Message type.</param>
        /// <param name="destination">Destination endpoint.</param>
        public void RouteToEndpoint(Type messageType, string destination)
        {
            GetOrCreate<UnicastRoutingTable>().RouteToEndpoint(messageType, destination);
        }

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