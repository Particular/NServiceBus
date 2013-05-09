namespace NServiceBus.Features
{
    /// <summary>
    /// Settings for the various features
    /// </summary>
    public class FeatureSettings
    {
        /// <summary>
        /// Enables the given feature
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public FeatureSettings Enable<T>() where T : Feature
        {
            Feature.Enable<T>();

            return this;
        }

        /// <summary>
        /// Disables the given feature
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public FeatureSettings Disable<T>() where T : Feature
        {
            Feature.Disable<T>();

            return this;
        }
    }
}