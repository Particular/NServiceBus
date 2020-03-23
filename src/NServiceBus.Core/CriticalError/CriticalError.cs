namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Logging;

    /// <summary>
    /// A holder for that exposes access to the action defined by <see cref="ConfigureCriticalErrorAction.DefineCriticalErrorAction(EndpointConfiguration, Func{ICriticalErrorContext, Task})"/>.
    /// </summary>
    /// <returns>
    /// Call <see cref="Raise"/> to trigger the action.
    /// </returns>
    public class CriticalError
    {
        /// <summary>
        /// Initializes a new instance of <see cref="CriticalError" />.
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
            return TaskEx.CompletedTask;
        }

        /// <summary>
        /// Trigger the action defined by
        /// <see
        ///     cref="ConfigureCriticalErrorAction.DefineCriticalErrorAction(EndpointConfiguration, Func{ICriticalErrorContext, Task})" />
        /// .
        /// </summary>
        public virtual void Raise(string errorMessage, Exception exception)
        {
            Guard.AgainstNullAndEmpty(nameof(errorMessage), errorMessage);
            Guard.AgainstNull(nameof(exception), exception);
            LogManager.GetLogger("NServiceBus").Fatal(errorMessage, exception);

            lock (endpointCriticalLock)
            {
                if (endpointStopCallback == null)
                {
                    criticalErrors.Add(new LatentCritical
                    {
                        Message = errorMessage,
                        Exception = exception
                    });
                    return;
                }
            }

            // don't await the criticalErrorAction in order to avoid deadlocks
            RaiseForEndpoint(errorMessage, exception);
        }

        void RaiseForEndpoint(string errorMessage, Exception exception)
        {
            Task.Run(() =>
            {
                var context = new CriticalErrorContext(endpointStopCallback, errorMessage, exception);
                return criticalErrorAction(context);
            }).Ignore();
        }

        /// <summary>
        /// Registers the callback to be called when critical error handler decides to stop the endpoint.
        /// </summary>
        public void SetStopCallback(Func<Task> endpointStopCallback)
        {
            lock (endpointCriticalLock)
            {
                this.endpointStopCallback = endpointStopCallback;
                foreach (var latentCritical in criticalErrors)
                {
                    RaiseForEndpoint(latentCritical.Message, latentCritical.Exception);
                }
                criticalErrors.Clear();
            }
        }

        Func<CriticalErrorContext, Task> criticalErrorAction;

        List<LatentCritical> criticalErrors = new List<LatentCritical>();
        Func<Task> endpointStopCallback;
        object endpointCriticalLock = new object();

        class LatentCritical
        {
            public string Message { get; set; }
            public Exception Exception { get; set; }
        }
    }
}