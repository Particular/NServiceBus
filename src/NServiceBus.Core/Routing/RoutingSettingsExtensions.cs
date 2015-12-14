namespace NServiceBus
{
    /// <summary>
    /// Configuration extensions for routing.
    /// </summary>
    public static class RoutingSettingsExtensions
    {
        /// <summary>
        /// Gets the routing table for the direct routing.
        /// </summary>
        public static RoutingSettings Routing(this BusConfiguration config)
        {
            Guard.AgainstNull(nameof(config), config);
            return new RoutingSettings(config.Settings);
        }
    }
}