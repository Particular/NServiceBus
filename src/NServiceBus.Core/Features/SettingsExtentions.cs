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
        public static void EnableFeatureByDefault<T>(this SettingsHolder settings) where T : Feature
        {
            settings.EnableFeatureByDefault(typeof(T));
        }

        /// <summary>
        /// Marks the given feature as enabled by default
        /// </summary>
        public static void EnableFeatureByDefault(this SettingsHolder settings, Type featureType)
        {
            settings.SetDefault(featureType.FullName, true);
        }
    }
}