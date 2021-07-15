namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Logging;

    /// <summary>
    /// A holder for that exposes access to the action defined by <see cref="ConfigureCriticalErrorAction.DefineCriticalErrorAction(EndpointConfiguration, Func{ICriticalErrorContext, CancellationToken, Task})"/>.
    /// </summary>
    /// <returns>
    /// Call <see cref="Raise"/> to trigger the action.
    /// </returns>
    public partial class CriticalError
    {
        /// <summary>
        /// Initializes a new instance of <see cref="CriticalError" />.
        /// </summary>
        /// <param name="onCriticalErrorAction">The action to execute when a critical error is triggered.</param>
        public CriticalError(Func<ICriticalErrorContext, CancellationToken, Task> onCriticalErrorAction)
        {
            if (onCriticalErrorAction == null)
            {
                criticalErrorAction = (_, __) => Task.CompletedTask;
            }
            else
            {
                criticalErrorAction = onCriticalErrorAction;
            }
        }

        /// <summary>
        /// Trigger the action defined by
        /// <see
        ///     cref="ConfigureCriticalErrorAction.DefineCriticalErrorAction(EndpointConfiguration, Func{ICriticalErrorContext, CancellationToken, Task})" />
        /// .
        /// </summary>
        public virtual void Raise(string errorMessage, Exception exception = null, CancellationToken cancellationToken = default)
        {
            Guard.AgainstNullAndEmpty(nameof(errorMessage), errorMessage);
            var logger = LogManager.GetLogger("NServiceBus");
            if (exception != null)
            {
                logger.Fatal(errorMessage, exception);
            }
            else
            {
                logger.Fatal(errorMessage);
            }

            lock (endpointCriticalLock)
            {
                if (endpoint == null)
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
            RaiseForEndpoint(errorMessage, exception, cancellationToken);
        }

        void RaiseForEndpoint(string errorMessage, Exception exception, CancellationToken cancellationToken)
        {
            _ = Task.Run(() =>
            {
                var context = new CriticalErrorContext(endpoint.Stop, errorMessage, exception);
                return criticalErrorAction(context, cancellationToken);
            }, cancellationToken);
        }

        internal void SetEndpoint(IEndpointInstance endpointInstance, CancellationToken cancellationToken = default)
        {
            lock (endpointCriticalLock)
            {
                endpoint = endpointInstance;
                foreach (var latentCritical in criticalErrors)
                {
                    RaiseForEndpoint(latentCritical.Message, latentCritical.Exception, cancellationToken);
                }
                criticalErrors.Clear();
            }
        }

        Func<CriticalErrorContext, CancellationToken, Task> criticalErrorAction;

        List<LatentCritical> criticalErrors = new List<LatentCritical>();
        IEndpointInstance endpoint;
        object endpointCriticalLock = new object();

        class LatentCritical
        {
            public string Message { get; set; }
            public Exception Exception { get; set; }
        }
    }
}
