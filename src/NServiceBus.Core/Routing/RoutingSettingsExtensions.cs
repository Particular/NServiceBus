namespace NServiceBus
{
    /// <summary>
    /// Configuration extensions for routing.
    /// </summary>
    public static class RoutingSettingsExtensions
    {
        /// <summary>
        /// Controls the unicast routing.
        /// </summary>
        public static RoutingSettings Routing(this EndpointConfiguration config)
        {
            Guard.AgainstNull(nameof(config), config);
            return new RoutingSettings(config.Settings);
        }        
    }
}