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
        /// Stops the endpoint from processing new messages, giving the endpoint a period of time
        /// to gracefuly complete message processing on any in-flight messages before forcefully
        /// cancelling processing on message handlers that exceed the timeout.
        /// </summary>
        /// <param name="endpoint">The endpoint to stop.</param>
        /// <param name="gracefulShutdownTimeout">
        /// A length of time to allow messages that have already begun processing to complete naturally before throwing
        /// <see cref="OperationCanceledException"/> to forcefully abort them.
        /// </param>
        public static Task Stop(this IEndpointInstance endpoint, TimeSpan gracefulShutdownTimeout) =>
            endpoint.Stop(new CancellationTokenSource(gracefulShutdownTimeout).Token);
    }
}
