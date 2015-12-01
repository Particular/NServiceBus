namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// The signature of the a critical error handler used by <see cref="ConfigureCriticalErrorAction.DefineCriticalErrorAction"/>.
    /// </summary>
    /// <param name="endpoint">The instance of <see cref="IEndpointInstance"/> that cause the error.</param>
    /// <param name="error">A description of the error.</param>
    /// <param name="exception">The last <see cref="Exception"/> that cause the error.</param>
    public delegate Task CriticalErrorAction(IEndpointInstance endpoint, string error, Exception exception);
}