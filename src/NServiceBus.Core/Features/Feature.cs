namespace NServiceBus.Features
{
    using System;
    using Settings;

    /// <summary>
    /// Used to control the various features supported by the framewrok
    /// </summary>
    public class Feature
    {
        /// <summary>
        /// Enables the give feature
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static void Enable<T>() where T:IFeature
        {
            Enable(typeof(T));
        }

        // <summary>
        /// Enables the give feature
        /// </summary>
        public static void Enable(Type featureType) 
        {
            SettingsHolder.Set(featureType.FullName, true);
        }

        /// <summary>
        /// Turns the given feature off
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static void Disable<T>() where T : IFeature
        {
           Disable(typeof(T));
        }

        /// <summary>
        /// Turns the given feature off
        /// </summary>
        public static void Disable(Type featureType)
        {
            SettingsHolder.Set(featureType.FullName, false);
        }


        /// <summary>
        /// Returns true if the given feature is enabled
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static bool IsEnabled<T>() where T : IFeature
        {
            return IsEnabled(typeof (T));
        }


        /// <summary>
        /// Returns true if the given feature is enabled
        /// </summary>
        /// <param name="feature"></param>
        /// <returns></returns>
        public static bool IsEnabled(Type feature)
        {
            return SettingsHolder.GetOrDefault<bool>(feature.FullName);
        }


    }
}