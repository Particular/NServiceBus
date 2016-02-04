namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// The context of a critical error handler used by <see cref="ConfigureCriticalErrorAction.DefineCriticalErrorAction(EndpointConfiguration, Func{ICriticalErrorContext, Task})"/>.
    /// </summary>
    public interface ICriticalErrorContext
    {
        /// <summary>
        /// A delegate that optionally stops the endpoint. By default this is a pointer <see cref="IEndpointInstance.Stop"/>.
        /// </summary>
        Func<Task> Stop { get; }

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