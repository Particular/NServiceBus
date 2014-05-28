namespace NServiceBus
{
    using System;
    using Features;
    using Settings;

    /// <summary>
    /// Configuration extention that allows users fine grained control over the features
    /// </summary>
    public static class FeatureExtensions
    {

        /// <summary>
        /// Provides fine grained control for features
        /// </summary>
        /// <param name="config"></param>
        /// <param name="customizations"></param>
        /// <returns></returns>
        public static Configure Features(this Configure config,Action<FeatureSettings> customizations)
        {
            customizations(new FeatureSettings(config.Settings));

            return config;
        }

        /// <summary>
        /// Configuration actions available to features
        /// </summary>
        public class FeatureSettings
        {
            readonly SettingsHolder settings;

            public FeatureSettings(SettingsHolder settings)
            {
                this.settings = settings;
            }

            /// <summary>
            /// Enables the given feature
            /// </summary>
            public void Enable<T>() where T : Feature
            {
                Enable(typeof(T));
            }

            /// <summary>
            /// Enables the given feature
            /// </summary>
            /// <param name="featureType"></param>
            public void Enable(Type featureType)
            {
                settings.Set(featureType.FullName, true);
            }

            /// <summary>
            /// Disables the given feature
            /// </summary>
            public void Disable<T>() where T : Feature
            {
                Disable(typeof(T));
            }

            /// <summary>
            /// Disables the give feature
            /// </summary>
            /// <param name="featureType"></param>
            /// <returns></returns>
            public void Disable(Type featureType)
            {
                settings.Set(featureType.FullName, false);
            }

        }
    }
}
