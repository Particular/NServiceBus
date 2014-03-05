namespace NServiceBus
{
    using System;
    using Features;
    using SecondLevelRetries.Config;

    public static class FeatureSettingsExtensions
    {
        public static FeatureSettings SecondLevelRetries(this FeatureSettings settings, Action<SecondLevelRetriesSettings> customSettings)
        {
            customSettings(new SecondLevelRetriesSettings());

            return settings;
        }
    }
}