namespace NServiceBus
{
    using System;
    using Features;

    /// <summary>
    /// Extension methods declarations.
    /// </summary>
    public static class ConfigurationBuilderExtensions
    {
        /// <summary>
        /// Enables the given feature
        /// </summary>
        public static void EnableFeature<T>(this BusConfiguration config) where T : Feature
        {
            Guard.AgainstNull(config, "config");
            config.EnableFeature(typeof(T));
        }

        /// <summary>
        /// Enables the given feature
        /// </summary>
        /// <param name="config"></param>
        /// <param name="featureType">The feature to enable</param>
        public static void EnableFeature(this BusConfiguration config, Type featureType)
        {
            Guard.AgainstNull(config, "config");
            Guard.AgainstNull(featureType, "featureType");
            config.Settings.Set(featureType.FullName, true);
        }

        /// <summary>
        /// Disables the given feature
        /// </summary>
        public static void DisableFeature<T>(this BusConfiguration config) where T : Feature
        {
            Guard.AgainstNull(config, "config");
            config.DisableFeature(typeof(T));
        }

        /// <summary>
        /// Enables the given feature
        /// </summary>
        /// <param name="config"></param>
        /// <param name="featureType">The feature to disable</param>
        public static void DisableFeature(this BusConfiguration config, Type featureType)
        {
            Guard.AgainstNull(config, "config");
            Guard.AgainstNull(featureType, "featureType");
            config.Settings.Set(featureType.FullName, false);
        }
    }
}
