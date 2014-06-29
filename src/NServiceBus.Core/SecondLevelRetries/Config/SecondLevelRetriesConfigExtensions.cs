namespace NServiceBus
{
    using System;
    using SecondLevelRetries.Config;

    /// <summary>
    /// Provides config options for the SLR feature
    /// </summary>
    public static class SecondLevelRetriesConfigExtensions
    {
        /// <summary>
        /// Allows for customization of the second level retries
        /// </summary>
        public static Configure SecondLevelRetries(this Configure config, Action<SecondLevelRetriesSettings> customSettings)
        {
            customSettings(new SecondLevelRetriesSettings(config));

            return config;
        }
    }
}