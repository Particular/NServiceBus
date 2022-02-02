namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents an endpoint in the running phase.
    /// </summary>
    public interface IEndpointInstance : IMessageSession, IAsyncDisposable
    {
        /// <summary>
        /// Stops the endpoint.
        /// </summary>
        Task Stop(CancellationToken cancellationToken = default);
    }
}