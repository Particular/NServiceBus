namespace NServiceBus
{
    /// <summary>
    /// Configuration extensions for routing feature settings.
    /// </summary>
    public static class RoutingFeatureSettingsExtensions
    {
        /// <summary>
        /// Sets the public return address of this endpoint.
        /// </summary>
        /// <param name="configuration">The endpoint configuration to extend.</param>
        /// <param name="address">The public return address for messages sent by this endpoint.</param>
        public static void OverridePublicReturnAddress(this EndpointConfiguration configuration, string address)
        {
            Guard.AgainstNullAndEmpty(nameof(address), address);
            configuration.Settings.SetDefault("PublicReturnAddress", address);
        }
    }
}