namespace NServiceBus
{
    using System;

    /// <summary>
    /// See <see cref="ICriticalErrorContext"/>.
    /// </summary>
    public class CriticalErrorContext: ICriticalErrorContext
    {
        /// <summary>
        /// Initizes a new instance of <see cref="CriticalErrorContext"/>.
        /// </summary>
        /// <param name="endpointInstance">See <see cref="ICriticalErrorContext.EndpointInstance"/>.</param>
        /// <param name="error">See <see cref="ICriticalErrorContext.Error"/>.</param>
        /// <param name="exception">See <see cref="ICriticalErrorContext.Exception"/>.</param>
        public CriticalErrorContext(IEndpointInstance endpointInstance, string error, Exception exception)
        {
            Guard.AgainstNull(nameof(endpointInstance),endpointInstance);
            Guard.AgainstNullAndEmpty(nameof(error), error);
            Guard.AgainstNull(nameof(exception), exception);
            EndpointInstance = endpointInstance;
            Error = error;
            Exception = exception;
        }
        
        /// <summary>
        /// See <see cref="ICriticalErrorContext.EndpointInstance"/>.
        /// </summary>
        public IEndpointInstance EndpointInstance { get; }
        
        /// <summary>
        /// See <see cref="ICriticalErrorContext.Error"/>.
        /// </summary>
        public string Error { get; }

        /// <summary>
        /// See <see cref="ICriticalErrorContext.Exception"/>.
        /// </summary>
        public Exception Exception { get; }
    }
}