namespace NServiceBus.Features
{
    /// <summary>
    /// Base for feature startup tasks.
    /// </summary>
    public abstract class FeatureStartupTask
    {
        /// <summary>
        /// Will be called when the endpoint starts up if the feature has been activated.
        /// </summary>
        protected abstract void OnStart();
        
        /// <summary>
        ///  Will be called when the endpoint stops and the feature is active.
        /// </summary>
        protected virtual void OnStop(){}
        
        internal void PerformStartup()
        {
            OnStart();
        }

        internal void PerformStop()
        {
            OnStop();
        }
    }
}