namespace NServiceBus
{
    using System;
    using SecondLevelRetries.Config;

    public static class FeatureSettingsExtensions
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