namespace NServiceBus.Features
{
    /// <summary>
    /// Notifications feature
    /// </summary>
    public class Notifications : IConditionalFeature
    {
        /// <summary>
        /// Returns true if the feature should be enable. This method wont be called if the feature is explicitly disabled
        /// </summary>
        /// <returns></returns>
        public bool ShouldBeEnabled()
        {
            return !Configure.Instance.IsConfiguredAsMasterNode();
        }

        /// <summary>
        /// Called when the feature should perform its initialization. This call will only happen if the feature is enabled.
        /// </summary>
        public void Initialize()
        {

        }
    }
}