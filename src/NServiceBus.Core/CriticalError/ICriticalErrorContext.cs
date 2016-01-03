namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// The context of a critical error handler used by <see cref="ConfigureCriticalErrorAction.DefineCriticalErrorAction(BusConfiguration, Func{CriticalErrorContext, Task})"/>.
    /// </summary>
    public interface ICriticalErrorContext
    {
        /// <summary>
        /// The instance of <see cref="IEndpointInstance"/> that cause the error.
        /// </summary>
        IEndpointInstance EndpointInstance { get; }

        /// <summary>
        /// A description of the error.
        /// </summary>
        string Error { get; }

        /// <summary>
        /// The last <see cref="Exception"/> that cause the error.
        /// </summary>
        Exception Exception { get; }
    }
}