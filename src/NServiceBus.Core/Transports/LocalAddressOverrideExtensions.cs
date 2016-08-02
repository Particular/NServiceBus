namespace NServiceBus
{
    /// <summary>
    /// Provides extension methods to configure the local input address.
    /// </summary>
    public static class LocalAddressOverrideExtensions
    {
        /// <summary>
        /// Changes the name of the input address to the specified value instead of using the configured endpoint name.
        /// </summary>
        /// <param name="configuration">The endpoint configuration.</param>
        /// <param name="addressOverride">The input address to use instead.</param>
        public static void OverrideLocalAddress(this EndpointConfiguration configuration, string addressOverride)
        {
            configuration.Settings.Set(Receiving.SharedQueueAddressOverrideSettingsKey, addressOverride);
        }
    }
}