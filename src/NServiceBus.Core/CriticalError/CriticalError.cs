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
        public virtual void Raise(string errorMessage, Exception exception, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);
            ArgumentNullException.ThrowIfNull(exception);
            LogManager.GetLogger("NServiceBus").Fatal(errorMessage, exception);

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
            var context = new CriticalErrorContext(endpoint.Stop, errorMessage, exception);

            _ = RaiseCriticalError(context, criticalErrorAction, cancellationToken);
            return;

            static async Task RaiseCriticalError(CriticalErrorContext errorContext,
                Func<CriticalErrorContext, CancellationToken, Task> criticalErrorAction,
                CancellationToken cancellationToken)
            {
                await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
                await criticalErrorAction(errorContext, cancellationToken).ConfigureAwait(false);
            }
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

        readonly Func<CriticalErrorContext, CancellationToken, Task> criticalErrorAction;

        readonly List<LatentCritical> criticalErrors = new List<LatentCritical>();
        IEndpointInstance endpoint;
        readonly object endpointCriticalLock = new object();

        class LatentCritical
        {
            public string Message { get; set; }
            public Exception Exception { get; set; }
        }
    }
}