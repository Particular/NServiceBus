namespace NServiceBus.Performance.TimeToBeReceived
{
    using System;
    using DeliveryConstraints;

    /// <summary>
    /// Instructs the transport to discard the message if it hasn't been received.
    /// within the specified timespan.
    /// </summary>
    public class DiscardIfNotReceivedBefore : DeliveryConstraint
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