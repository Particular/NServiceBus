namespace NServiceBus
{
    /// <summary>
    /// Provides config options for the FLR feature.
    /// </summary>
    public static class FirstLevelRetriesConfigExtensions
    {
        /// <summary>
        /// Allows for customization of the first level retries.
        /// </summary>
        /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
        public static FirstLevelRetriesSettings FirstLevelRetries(this EndpointConfiguration config)
        {
            Guard.AgainstNull(nameof(config), config);
            return new FirstLevelRetriesSettings(config);
        }
    }
}