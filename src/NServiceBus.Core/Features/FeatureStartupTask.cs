namespace NServiceBus.Features
{
    using System.Threading;
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
        protected abstract Task OnStart(IMessageSession session, CancellationToken cancellationToken = default);

        /// <summary>
        /// Will be called after an endpoint has been stopped and no longer processes new incoming messages. This method is only invoked if the feature has been
        /// activated.
        /// </summary>
        protected abstract Task OnStop(IMessageSession session, CancellationToken cancellationToken = default);

        internal Task PerformStartup(IMessageSession session, CancellationToken cancellationToken = default)
        {
            messageSession = session;
            return OnStart(session, cancellationToken);
        }

        internal Task PerformStop(CancellationToken cancellationToken = default)
        {
            return OnStop(messageSession, cancellationToken);
        }

        IMessageSession messageSession;
    }
}