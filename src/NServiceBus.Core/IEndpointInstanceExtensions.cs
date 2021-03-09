namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Extensions for <see cref="IEndpointInstance"/>.
    /// </summary>
    public static class IEndpointInstanceExtensions
    {
        /// <summary>
        /// Stops the endpoint from processing new messages,
        /// granting a period of time to gracefully complete processing before forceful cancellation.
        /// </summary>
        /// <param name="endpoint">The endpoint to stop.</param>
        /// <param name="gracefulStopTimeout">The length of time granted to gracefully complete processing.</param>
        public static Task Stop(this IEndpointInstance endpoint, TimeSpan gracefulStopTimeout) =>
            endpoint.Stop(new CancellationTokenSource(gracefulStopTimeout).Token);
    }
}
