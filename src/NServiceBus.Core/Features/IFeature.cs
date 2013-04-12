namespace NServiceBus.Features
{
    /// <summary>
    /// Defines a framework feature
    /// </summary>
    public interface IFeature
    {
        /// <summary>
        /// Called when the feature should perform its initialization. This call will only happen if the feature is enabled.
        /// </summary>
        void Initialize();
    }
}