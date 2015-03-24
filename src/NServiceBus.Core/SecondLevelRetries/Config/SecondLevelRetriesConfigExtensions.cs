namespace NServiceBus
{
    using SecondLevelRetries.Config;

    /// <summary>
    /// Provides config options for the SLR feature
    /// </summary>
    public static class SecondLevelRetriesConfigExtensions
    {
        /// <summary>
        /// Allows for customization of the second level retries
        /// </summary>
        public static SecondLevelRetriesSettings SecondLevelRetries(this BusConfiguration config)
        {
            Guard.AgainstNull(config, "config");
            return new SecondLevelRetriesSettings(config);
        }
    }
}