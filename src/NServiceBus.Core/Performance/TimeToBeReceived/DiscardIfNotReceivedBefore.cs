namespace NServiceBus.Performance.TimeToBeReceived
{
    using System;

    /// <summary>
    /// Instructs the transport to discard the message if it hasn't been received
    /// within the specified <see cref="TimeSpan"/>.
    /// </summary>
    public class DiscardIfNotReceivedBefore
    {
        /// <summary>
        /// Initializes the constraint with a max time.
        /// </summary>
        public DiscardIfNotReceivedBefore(TimeSpan maxTime)
        {
            MaxTime = maxTime;
        }

        /// <summary>
        /// The max time to wait before discarding the message.
        /// </summary>
        public TimeSpan MaxTime { get; }
    }
}