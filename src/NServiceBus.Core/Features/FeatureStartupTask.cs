namespace NServiceBus.Features
{
    using System.Threading.Tasks;

    /// <summary>
    /// Base for feature startup tasks.
    /// </summary>
    public abstract class FeatureStartupTask
    {
        /// <summary>
        /// Will be called after an endpoint has been started but before processing any messages, if the feature has been
        /// activated.
        /// </summary>
        /// <param name="session">Bus session.</param>
        protected abstract Task OnStart(IMessageSession session);

        /// <summary>
        /// Will be called after an endpoint has been started but before processing any messages, if the feature has been
        /// activated.
        /// </summary>
        /// <param name="session">Bus session.</param>
        protected abstract Task OnStop(IMessageSession session);

        internal Task PerformStartup(IMessageSession session)
        {
            return OnStart(session);
        }

        internal Task PerformStop(IMessageSession session)
        {
            return OnStop(session);
        }
    }
}