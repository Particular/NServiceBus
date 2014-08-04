namespace NServiceBus
{
    using System;
    using System.Threading;
    using NServiceBus.Logging;

    /// <summary>
    /// A holder for that exposes access to the action defined by <see cref="ConfigureCriticalErrorAction.DefineCriticalErrorAction(Configure,Action{string,Exception})"/>.
    /// </summary>
    /// <returns>
    /// Call <see cref="Raise"/> to trigger the action.
    /// </returns>
    public class CriticalError
    {
        Action<string, Exception> onCriticalErrorAction;
        Configure configure;

        internal CriticalError(Action<string, Exception> onCriticalErrorAction, Configure configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException("configure");
            }
            this.onCriticalErrorAction = onCriticalErrorAction;
            this.configure = configure;
        }

        void DefaultCriticalErrorHandling()
        {
            if (!configure.Configurer.HasComponent<IBus>())
            {
                return;
            }

            configure.Builder.Build<IStartableBus>()
                .Shutdown();
        }

        /// <summary>
        /// Trigger the action defined by <see cref="ConfigureCriticalErrorAction.DefineCriticalErrorAction(Configure,Action{string,Exception})"/>.
        /// </summary>
        public void Raise(string errorMessage, Exception exception)
        {
            LogManager.GetLogger("NServiceBus").Fatal(errorMessage, exception);

            if (onCriticalErrorAction == null)
            {
                ThreadPool.QueueUserWorkItem(state => DefaultCriticalErrorHandling());
            }
            else
            {
                ThreadPool.QueueUserWorkItem(state => onCriticalErrorAction(errorMessage, exception));
            }
        }
    }
}