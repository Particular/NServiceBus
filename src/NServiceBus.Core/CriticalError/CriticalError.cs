namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Logging;

    /// <summary>
    /// A holder for that exposes access to the action defined by <see cref="ConfigureCriticalErrorAction.DefineCriticalErrorAction(BusConfiguration, Func{ICriticalErrorContext, Task})"/>.
    /// </summary>
    /// <returns>
    /// Call <see cref="Raise"/> to trigger the action.
    /// </returns>
    public class CriticalError
    {
        Func<CriticalErrorContext, Task> criticalErrorAction;

        /// <summary>
        /// The started endpoint which will be passed to the configured critical error action.
        /// </summary>
        internal IEndpointInstance Endpoint { private get; set; }

        /// <summary>
        /// Initializes a new instance of <see cref="CriticalError"/>.
        /// </summary>
        /// <param name="onCriticalErrorAction">The action to execute when a critical error is triggered.</param>
        public CriticalError(Func<ICriticalErrorContext, Task> onCriticalErrorAction)
        {
            if (onCriticalErrorAction == null)
            {
                criticalErrorAction = DefaultCriticalErrorHandling;
            }
            else
            {
                criticalErrorAction = onCriticalErrorAction;
            }
        }

        static Task DefaultCriticalErrorHandling(ICriticalErrorContext criticalErrorContext)
        {
            return criticalErrorContext.EndpointInstance.Stop();
        }

        /// <summary>
        /// Trigger the action defined by <see cref="ConfigureCriticalErrorAction.DefineCriticalErrorAction(BusConfiguration, Func{ICriticalErrorContext, Task})"/>.
        /// </summary>
        public virtual void Raise(string errorMessage, Exception exception)
        {
            Guard.AgainstNullAndEmpty(nameof(errorMessage), errorMessage);
            Guard.AgainstNull(nameof(exception), exception);
            LogManager.GetLogger("NServiceBus").Fatal(errorMessage, exception);

            if (Endpoint == null)
            {
                throw new InvalidOperationException("You can only raise critical errors on a started endpoint but the endpoint wasn't started yet.");
            }

            // don't await the criticalErrorAction in order to avoid deadlocks
            Task.Run(() =>
            {
                var context = new CriticalErrorContext(Endpoint, errorMessage, exception);
                return criticalErrorAction(context);
            });
        }
    }
}