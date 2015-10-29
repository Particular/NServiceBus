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
        [ObsoleteEx(TreatAsErrorFromVersion = "6", RemoveInVersion = "7", ReplacementTypeOrMember = "OnStart")]
        protected virtual void OnStart()
        {
        }

        /// <summary>
        /// Will be called after an endpoint has been started but before processing any messages, if the feature has been activated.
        /// </summary>
        /// <param name="bus">Seond context.</param>
        protected abstract void OnStart(ISendOnlyBus bus);

        /// <summary>
        ///  Will be called after an endpoint has stopped processing messages, if the feature has been activated.
        /// </summary>
        protected virtual void OnStop(){}
        
        internal void PerformStartup(ISendOnlyBus bus)
        {
            OnStart(bus);
        }

        internal void PerformStop()
        {
            OnStop();
        }
    }
}