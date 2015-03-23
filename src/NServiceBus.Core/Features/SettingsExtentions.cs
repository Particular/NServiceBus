namespace NServiceBus.Features
{
    using System;
    using Settings;

    /// <summary>
    /// Feature related extentions to the settings
    /// </summary>
    public static class SettingsExtentions
    {
        /// <summary>
        /// Marks the given feature as enabled by default
        /// </summary>
        public static SettingsHolder EnableFeatureByDefault<T>(this SettingsHolder settings) where T : Feature
        {
            Guard.AgainstNull(settings, "settings");
            settings.EnableFeatureByDefault(typeof(T));
            return settings;
        }

        /// <summary>
        /// Marks the given feature as enabled by default
        /// </summary>
        public static SettingsHolder EnableFeatureByDefault(this SettingsHolder settings, Type featureType)
        {
            Guard.AgainstNull(settings, "settings");
            Guard.AgainstNull(featureType, "featureType");
            settings.SetDefault(featureType.FullName, true);
            return settings;
        }
    }
}