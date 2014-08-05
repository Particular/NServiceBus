namespace NServiceBus
{
    using System;
    using Features;

    /// <summary>
    /// Configuration extention that allows users fine grained control over the features
    /// </summary>
    public static class FeatureExtensions
    {
        /// <summary>
        /// Enables the given feature
        /// </summary>
        public static Configure EnableFeature<T>(this Configure config) where T : Feature
        {
            return config.EnableFeature(typeof(T));
        }

        /// <summary>
        /// Enables the given feature
        /// </summary>
        /// <param name="config"></param>
        /// <param name="featureType">The feature to enable</param>
        public static Configure EnableFeature(this Configure config, Type featureType)
        {
            config.Settings.Set(featureType.FullName, true);

            return config;
        }

        /// <summary>
        /// Disables the given feature
        /// </summary>
        public static Configure DisableFeature<T>(this Configure config) where T : Feature
        {
            return config.DisableFeature(typeof(T));
        }

        /// <summary>
        /// Enables the given feature
        /// </summary>
        /// <param name="config"></param>
        /// <param name="featureType">The feature to disable</param>
        public static Configure DisableFeature(this Configure config, Type featureType)
        {
            config.Settings.Set(featureType.FullName, false);

            return config;
        }

        /// <summary>
        /// Enables the given feature
        /// </summary>
        public static ConfigurationBuilder EnableFeature<T>(this ConfigurationBuilder config) where T : Feature
        {
            return config.EnableFeature(typeof(T));
        }

        /// <summary>
        /// Enables the given feature
        /// </summary>
        /// <param name="config"></param>
        /// <param name="featureType">The feature to enable</param>
        public static ConfigurationBuilder EnableFeature(this ConfigurationBuilder config, Type featureType)
        {
            config.settings.Set(featureType.FullName, true);

            return config;
        }

        /// <summary>
        /// Disables the given feature
        /// </summary>
        public static ConfigurationBuilder DisableFeature<T>(this ConfigurationBuilder config) where T : Feature
        {
            return config.DisableFeature(typeof(T));
        }

        /// <summary>
        /// Enables the given feature
        /// </summary>
        /// <param name="config"></param>
        /// <param name="featureType">The feature to disable</param>
        public static ConfigurationBuilder DisableFeature(this ConfigurationBuilder config, Type featureType)
        {
            config.settings.Set(featureType.FullName, false);

            return config;
        }
    }
}
