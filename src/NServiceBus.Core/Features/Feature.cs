namespace NServiceBus.Features
{
    using System;
    using Settings;

    /// <summary>
    /// Used to control the various features supported by the framework.
    /// </summary>
    public abstract class Feature
    {
        /// <summary>
        /// Called when the feature should perform its initialization. This call will only happen if the feature is enabled.
        /// </summary>
        public virtual void Initialize()
        {

        }

        /// <summary>
        /// Returns true if the feature should be enable. This method wont be called if the feature is explicitly disabled
        /// </summary>
        /// <returns></returns>
        public virtual bool ShouldBeEnabled()
        {
            return true;
        }

        /// <summary>
        /// Return <c>true</c> if this is a default <see cref="Feature"/> that needs to be turned on automatically.
        /// </summary>
        public virtual bool IsEnabledByDefault
        {
            get { return false; }
        }

        /// <summary>
        /// Feature name.
        /// </summary>
        public virtual string Name
        {
            get { return GetType().Name.Replace("Feature", String.Empty); }
        }

        /// <summary>
        /// Enables the give feature
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static void Enable<T>() where T : Feature
        {
            Enable(typeof (T));
        }

        // <summary>
        /// Enables the give feature
        /// </summary>
        public static void Enable(Type featureType)
        {
            SettingsHolder.Set(featureType.FullName, true);
        }

        // <summary>
        /// Enables the give feature unless explicitly disabled
        /// </summary>
        public static void EnableByDefault<T>() where T : Feature
        {
            EnableByDefault(typeof (T));
        }

        // <summary>
        /// Enables the give feature unless explicitly disabled
        /// </summary>
        public static void EnableByDefault(Type featureType)
        {
            SettingsHolder.SetDefault(featureType.FullName, true);
        }



        /// <summary>
        /// Turns the given feature off
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static void Disable<T>() where T : Feature
        {
            Disable(typeof (T));
        }

        /// <summary>
        /// Turns the given feature off
        /// </summary>
        public static void Disable(Type featureType)
        {
            SettingsHolder.Set(featureType.FullName, false);
        }

        // <summary>
        /// Disabled the give feature unless explicitly enabled
        /// </summary>
        public static void DisableByDefault(Type featureType)
        {
            SettingsHolder.SetDefault(featureType.FullName, false);
        }

        /// <summary>
        /// Returns true if the given feature is enabled
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static bool IsEnabled<T>() where T : Feature
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