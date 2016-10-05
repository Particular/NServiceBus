namespace NServiceBus.Features
{
    using System;
    using Settings;

    /// <summary>
    /// Feature related extensions to the settings.
    /// </summary>
    public static class SettingsExtensions
    {
        /// <summary>
        /// Marks the given feature as enabled by default.
        /// </summary>
        public static SettingsHolder EnableFeatureByDefault<T>(this SettingsHolder settings) where T : Feature
        {
            Guard.AgainstNull(nameof(settings), settings);
            settings.EnableFeatureByDefault(typeof(T));
            return settings;
        }

        /// <summary>
        /// Marks the given feature as enabled by default.
        /// </summary>
        public static SettingsHolder EnableFeatureByDefault(this SettingsHolder settings, Type featureType)
        {
            Guard.AgainstNull(nameof(settings), settings);
            Guard.AgainstNull(nameof(featureType), featureType);
            settings.SetDefault(featureType.FullName, FeatureState.Enabled);
            return settings;
        }

        /// <summary>
        /// Returns if a given feature has been activated in this endpoint.
        /// </summary>
        public static bool IsFeatureActive(this ReadOnlySettings settings, Type featureType)
        {
            return settings.GetOrDefault<FeatureState>(featureType.FullName) == FeatureState.Active;
        }

        /// <summary>
        /// Returns if a given feature has been enabled in this endpoint.
        /// </summary>
        public static bool IsFeatureEnabled(this ReadOnlySettings settings, Type featureType)
        {
            return settings.GetOrDefault<FeatureState>(featureType.FullName) == FeatureState.Enabled;
        }

        internal static void EnableFeature(this SettingsHolder settings, Type featureType)
        {
            settings.Set(featureType.FullName, FeatureState.Enabled);
        }

        internal static void DisableFeature(this SettingsHolder settings, Type featureType)
        {
            settings.Set(featureType.FullName, FeatureState.Disabled);
        }

        internal static void MarkFeatureAsActive(this SettingsHolder settings, Type featureType)
        {
            settings.Set(featureType.FullName, FeatureState.Active);
        }

        internal static void MarkFeatureAsDeactivated(this SettingsHolder settings, Type featureType)
        {
            settings.Set(featureType.FullName, FeatureState.Deactivated);
        }
    }
}