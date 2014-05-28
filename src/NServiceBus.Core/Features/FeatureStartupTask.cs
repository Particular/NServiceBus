namespace NServiceBus.Features
{
    /// <summary>
    /// Base for feature startup tasks
    /// </summary>
    public abstract class FeatureStartupTask
    {
        /// <summary>
        /// Will be called when the endpoint starts up if the feature has been activated
        /// </summary>
        protected abstract void OnStart();

        internal void PerformStartup()
        {
            OnStart();
        }
    }
}