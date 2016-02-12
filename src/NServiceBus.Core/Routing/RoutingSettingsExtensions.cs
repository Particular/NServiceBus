namespace NServiceBus
{
    using Configuration.AdvanceExtensibility;

    /// <summary>
    /// Configuration extensions for routing.
    /// </summary>
    public static class RoutingSettingsExtensions
    {
        /// <summary>
        /// Controls the unicast routing.
        /// </summary>
        public static UnicastRoutingSettings UnicastRouting(this ExposeSettings config)
        {
            Guard.AgainstNull(nameof(config), config);
            return new UnicastRoutingSettings(config.Settings);
        }        
    }
}