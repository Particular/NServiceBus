namespace NServiceBus.Features
{
    /// <summary>
    /// Notifications feature
    /// </summary>
    public class Notifications : Feature
    {
        /// <summary>
        /// Returns true if the feature should be enable. This method wont be called if the feature is explicitly disabled
        /// </summary>
        /// <returns></returns>
        public override bool ShouldBeEnabled()
        {
            return !Configure.Instance.IsConfiguredAsMasterNode();
        }

        /// <summary>
        /// Return <c>true</c> if this is a default <see cref="Feature"/> that needs to be turned on automatically.
        /// </summary>
        public override bool IsEnabledByDefault
        {
            get { return true; }
        }
    }
}