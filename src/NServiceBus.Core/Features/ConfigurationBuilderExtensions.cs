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
        public static void EnableFeature<T>(this ConfigurationBuilder config) where T : Feature
        {
            config.EnableFeature(typeof(T));
        }

        /// <summary>
        /// Enables the given feature
        /// </summary>
        /// <param name="config"></param>
        /// <param name="featureType">The feature to enable</param>
        public static void EnableFeature(this ConfigurationBuilder config, Type featureType)
        {
            config.settings.Set(featureType.FullName, true);
        }

        /// <summary>
        /// Disables the given feature
        /// </summary>
        public static void DisableFeature<T>(this ConfigurationBuilder config) where T : Feature
        {
            config.DisableFeature(typeof(T));
        }

        /// <summary>
        /// Enables the given feature
        /// </summary>
        /// <param name="config"></param>
        /// <param name="featureType">The feature to disable</param>
        public static void DisableFeature(this ConfigurationBuilder config, Type featureType)
        {
            config.settings.Set(featureType.FullName, false);
        }
    }
}
