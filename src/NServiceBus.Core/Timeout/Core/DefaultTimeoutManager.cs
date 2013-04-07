namespace NServiceBus.Timeout.Core
{
    using System;
    using Transports;

    /// <summary>
    /// Default implementation for <see cref="IManageTimeouts"/>
    /// </summary>
    public class DefaultTimeoutManager : IManageTimeouts
    {
        /// <summary>
        /// The timeout persister.
        /// </summary>
        public IPersistTimeouts TimeoutsPersister { get; set; }

        /// <summary>
        /// Messages sender.
        /// </summary>
        public ISendMessages MessageSender { get; set; }

        /// <summary>
        /// Fires when a timeout is added.
        /// </summary>
        public event EventHandler<TimeoutData> TimeoutPushed;

        /// <summary>
        /// Adds a new timeout to be monitored.
        /// </summary>
        /// <param name="timeout"><see cref="TimeoutData"/> to be added.</param>
        public void PushTimeout(TimeoutData timeout)
        {
            if (timeout.Time.AddSeconds(-1) <= DateTime.UtcNow)
            {
                MessageSender.Send(timeout.ToTransportMessage(), timeout.Destination);
                return;
            }

            TimeoutsPersister.Add(timeout);

            if (TimeoutPushed != null)
            {
                TimeoutPushed.BeginInvoke(this, timeout, ar => {}, null);
            }
        }

        /// <summary>
        /// Removes a timeout from being monitored.
        /// </summary>
        /// <param name="timeoutId">The timeout id to be removed.</param>
        public void RemoveTimeout(string timeoutId)
        {
            TimeoutData timeoutData;

            TimeoutsPersister.TryRemove(timeoutId, out timeoutData);
        }

        /// <summary>
        /// Clears the timeout for the given <paramref name="sagaId"/>.
        /// </summary>
        /// <param name="sagaId">The sagaId to be removed</param>
        public void RemoveTimeoutBy(Guid sagaId)
        {
            TimeoutsPersister.RemoveTimeoutBy(sagaId);
        }
    }
}
