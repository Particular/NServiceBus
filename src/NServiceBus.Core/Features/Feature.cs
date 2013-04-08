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
            SettingsHolder.Set(typeof(T).FullName,true);
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

        public static bool IsEnabled(Type feature)
        {
            return SettingsHolder.GetOrDefault<bool>(feature.FullName);
        }
    }
}