namespace NServiceBus
{
    using Transport;

    /// <summary>
    /// Configuration extensions for routing.
    /// </summary>
    public static class RoutingSettingsExtensions
    {
        /// <summary>
        /// Configures the routing.
        /// </summary>
        public static RoutingSettings Routing(this TransportExtensions config)
        {
            Guard.AgainstNull(nameof(config), config);
            return new RoutingSettings(config.Settings);
        }

        /// <summary>
        /// Configures the routing.
        /// </summary>
        public static RoutingSettings<T> Routing<T>(this TransportExtensions<T> config)
            where T : TransportDefinition
        {
            Guard.AgainstNull(nameof(config), config);
            return new RoutingSettings<T>(config.Settings);
        }
    }
}