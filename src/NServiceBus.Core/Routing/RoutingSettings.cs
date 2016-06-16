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
        /// <param name="destinationEndpoint">Destination endpoint.</param>
        public void RouteTo(Type messageType, string destinationEndpoint)
        {
            Settings.GetOrCreate<UnicastRoutingTable>().RouteToEndpoint(messageType, destinationEndpoint);
        }
    }

    /// <summary>
    /// Exposes settings related to routing.
    /// </summary>
    public class RoutingSettings<T> : RoutingSettings
        where T : TransportDefinition
    {
        internal RoutingSettings(SettingsHolder settings)
            : base(settings)
        {
        }
    }
}