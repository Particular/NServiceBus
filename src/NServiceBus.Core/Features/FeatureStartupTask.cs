namespace NServiceBus.Features
{
    using System.Threading.Tasks;

    /// <summary>
    /// Base for feature startup tasks.
    /// </summary>
    public abstract class FeatureStartupTask
    {
        /// <summary>
        /// Will be called after an endpoint has been started but before processing any messages, if the feature has been activated.
        /// </summary>
        /// <param name="session">Bus session.</param>
        protected abstract Task OnStart(IBusSession session);

        /// <summary>
        /// Will be called after an endpoint has been started but before processing any messages, if the feature has been activated.
        /// </summary>
        /// <param name="session">Bus session.</param>
        protected abstract Task OnStop(IBusSession session);
        
        internal Task PerformStartup(IBusSession session)
        {
            return OnStart(session);
        }

        internal Task PerformStop(IBusSession session)
        {
            return OnStop(session);
        }
    }
}