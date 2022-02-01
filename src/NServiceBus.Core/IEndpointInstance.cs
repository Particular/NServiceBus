namespace NServiceBus
{
#if NETCOREAPP
    using System;
#endif

    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents an endpoint in the running phase.
    /// </summary>
    public interface IEndpointInstance : IMessageSession
#if NETCOREAPP
    , IAsyncDisposable
#endif
    {
        /// <summary>
        /// Stops the endpoint.
        /// </summary>
        Task Stop(CancellationToken cancellationToken = default);

#if NETCOREAPP
        ValueTask IAsyncDisposable.DisposeAsync() => new ValueTask(Stop(CancellationToken.None));
#endif
    }
}