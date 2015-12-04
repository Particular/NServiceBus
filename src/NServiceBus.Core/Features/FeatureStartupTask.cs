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
        /// <param name="context">Bus context.</param>
        protected abstract Task OnStart(IBusContext context);

        /// <summary>
        /// Will be called after an endpoint has been started but before processing any messages, if the feature has been activated.
        /// </summary>
        /// <param name="context">Bus context.</param>
        protected virtual Task OnStop(IBusContext context)
        {
            return TaskEx.Completed;
        }
        
        internal Task PerformStartup(IBusContext context)
        {
            return OnStart(context);
        }

        internal Task PerformStop(IBusContext context)
        {
            return OnStop(context);
        }
    }
}