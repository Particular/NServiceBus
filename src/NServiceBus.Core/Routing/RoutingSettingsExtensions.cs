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
        public static RoutingSettings Routing(this EndpointConfiguration config)
        {
            Guard.AgainstNull(nameof(config), config);
            return new RoutingSettings(config.Settings);
        }
    }
}