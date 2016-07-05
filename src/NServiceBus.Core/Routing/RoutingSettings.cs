namespace NServiceBus
{
    using System;
    using Configuration.AdvanceExtensibility;
    using Routing;
    using Settings;
    using Transports;

    /// <summary>
    /// Exposes settings related to routing.
    /// </summary>
    public class RoutingSettings : ExposeSettings
    {
        internal RoutingSettings(SettingsHolder settings)
            : base(settings)
        {
        }

        /// <summary>
        /// Adds a static unicast route.
        /// </summary>
        /// <param name="messageType">Message type.</param>
        /// <param name="destination">Destination endpoint.</param>
        public RoutingSettings RouteToEndpoint(Type messageType, string destination)
        {
            Settings.GetOrCreate<UnicastRoutingTable>().RouteToEndpoint(messageType, destination);
            return this;
        }
    }

    /// <summary>
    /// Exposes settings related to routing.
    /// </summary>
    public class RoutingSettings<T> : ExposeSettings where T : TransportDefinition
    {
        internal RoutingSettings(SettingsHolder settings)
            : base(settings)
        {
        }

        /// <summary>
        /// Adds a static unicast route.
        /// </summary>
        /// <param name="messageType">Message type.</param>
        /// <param name="destination">Destination endpoint.</param>
        public RoutingSettings<T> RouteToEndpoint(Type messageType, string destination)
        {
            Settings.GetOrCreate<UnicastRoutingTable>().RouteToEndpoint(messageType, destination);
            return this;
        }
    }
}