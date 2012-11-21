namespace NServiceBus.Timeout.Core
{
    using System;

    /// <summary>
    /// Manages NSB timeouts.
    /// </summary>
    /// <remarks>Implementors must be thread-safe.</remarks>
    public interface IManageTimeouts
    {
        /// <summary>
        /// Adds a new timeout to be monitored.
        /// </summary>
        /// <param name="timeout"><see cref="TimeoutData"/> to be added.</param>
        void PushTimeout(TimeoutData timeout);

        /// <summary>
        /// Removes a timeout from being monitored.
        /// </summary>
        /// <param name="timeoutId">The timeout id to be removed.</param>
        void RemoveTimeout(string timeoutId);

        /// <summary>
        /// Clears the timeout for the given <paramref name="sagaId"/>.
        /// </summary>
        /// <param name="sagaId">The sagaId to be removed</param>
        void RemoveTimeoutBy(Guid sagaId);

        /// <summary>
        /// Fires when a timeout is added.
        /// </summary>
        event EventHandler<TimeoutData> TimeoutPushed;
    }
}
