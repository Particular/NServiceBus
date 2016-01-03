namespace NServiceBus
{
    using System;

    /// <summary>
    /// See <see cref="ICriticalErrorContext"/>.
    /// </summary>
    public class CriticalErrorContext: ICriticalErrorContext
    {
        internal CriticalErrorContext(IEndpointInstance endpointInstance, string error, Exception exception)
        {
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