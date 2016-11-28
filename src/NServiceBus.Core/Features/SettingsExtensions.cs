namespace NServiceBus.Features
{
    using System;
    using Settings;

    /// <summary>
    /// Feature related extensions to the settings.
    /// </summary>
    public static class SettingsExtensions
    {
        const string FeatureStateKey = "FeatureState:";
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
            settings.SetDefault(FeatureStateKey + featureType.FullName, FeatureState.Enabled);
            return settings;
        }

        /// <summary>
        /// Returns if a given feature has been activated in this endpoint.
        /// </summary>
        public static bool IsFeatureActive(this ReadOnlySettings settings, Type featureType)
        {
            return settings.GetFeatureState(featureType) == FeatureState.Active;
        }

        /// <summary>
        /// Returns if a given feature has been enabled in this endpoint.
        /// </summary>
        public static bool IsFeatureEnabled(this ReadOnlySettings settings, Type featureType)
        {
            return settings.GetFeatureState(featureType) == FeatureState.Enabled;
        }

        internal static void EnableFeature(this SettingsHolder settings, Type featureType)
        {
            settings.SetFeatureState(featureType, FeatureState.Enabled);
        }

        internal static void DisableFeature(this SettingsHolder settings, Type featureType)
        {
            settings.SetFeatureState(featureType, FeatureState.Disabled);
        }

        internal static void MarkFeatureAsActive(this SettingsHolder settings, Type featureType)
        {
            settings.SetFeatureState(featureType, FeatureState.Active);
        }

        internal static void MarkFeatureAsDeactivated(this SettingsHolder settings, Type featureType)
        {
            settings.SetFeatureState(featureType, FeatureState.Deactivated);
        }

        static void SetFeatureState(this SettingsHolder settings, Type featureType, FeatureState state)
        {
            settings.Set(FeatureStateKey + featureType.FullName, state);
        }

        static FeatureState GetFeatureState(this ReadOnlySettings settings, Type featureType)
        {
            return settings.GetOrDefault<FeatureState>(FeatureStateKey + featureType.FullName);
        }
    }
}