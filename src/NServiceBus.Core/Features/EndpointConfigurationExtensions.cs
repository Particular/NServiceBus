namespace NServiceBus
{
    using System;
    using Features;

    /// <summary>
    /// Extension methods declarations.
    /// </summary>
    public static class EndpointConfigurationExtensions
    {
        /// <summary>
        /// Enables the given feature.
        /// </summary>
        /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
        public static void EnableFeature<T>(this EndpointConfiguration config) where T : Feature
        {
            Guard.AgainstNull(nameof(config), config);
            config.EnableFeature(typeof(T));
        }

        /// <summary>
        /// Enables the given feature.
        /// </summary>
        /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
        /// <param name="featureType">The feature to enable.</param>
        public static void EnableFeature(this EndpointConfiguration config, Type featureType)
        {
            Guard.AgainstNull(nameof(config), config);
            Guard.AgainstNull(nameof(featureType), featureType);

            config.Settings.EnableFeature(featureType);
        }

        /// <summary>
        /// Disables the given feature.
        /// </summary>
        /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
        public static void DisableFeature<T>(this EndpointConfiguration config) where T : Feature
        {
            Guard.AgainstNull(nameof(config), config);
            config.DisableFeature(typeof(T));
        }

        /// <summary>
        /// Enables the given feature.
        /// </summary>
        /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
        /// <param name="featureType">The feature to disable.</param>
        public static void DisableFeature(this EndpointConfiguration config, Type featureType)
        {
            Guard.AgainstNull(nameof(config), config);
            Guard.AgainstNull(nameof(featureType), featureType);

            config.Settings.DisableFeature(featureType);
        }
    }
}