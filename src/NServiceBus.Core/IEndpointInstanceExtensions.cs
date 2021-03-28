namespace NServiceBus
{
    using System;
    using System.Diagnostics.CodeAnalysis;
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
        [SuppressMessage("Code", "PCR0019:A task-returning method should have a CancellationToken parameter or a parameter implementing ICancellableContext", Justification = "Convenience method wrapping the CancellationToken overload.")]
        public static Task Stop(this IEndpointInstance endpoint, TimeSpan gracefulStopTimeout) =>
            endpoint.Stop(new CancellationTokenSource(gracefulStopTimeout).Token);
    }
}
