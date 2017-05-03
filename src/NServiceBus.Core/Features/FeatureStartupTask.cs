namespace NServiceBus.Features
{
    using System.Threading.Tasks;

    /// <summary>
    /// Base for feature startup tasks.
    /// </summary>
    public abstract class FeatureStartupTask
    {
        /// <summary>
        /// Will be called after an endpoint has been started but before processing any messages. This method is only invoked if the feature has been
        /// activated.
        /// </summary>
        protected abstract Task OnStart(IMessageSession session);

        /// <summary>
        /// Will be called after an endpoint has been stopped and no longer processes new incoming messages. This method is only invoked if the feature has been
        /// activated.
        /// </summary>
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