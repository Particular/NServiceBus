namespace NServiceBus.Performance.TimeToBeReceived
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.DeliveryConstraints;

    /// <summary>
    /// Instructs the transport to discard the message if it hasn't been received.
    /// within the specified timespan.
    /// </summary>
    public class DiscardIfNotReceivedBefore : DeliveryConstraint
    {
        /// <summary>
        /// The max time to wait before discarding the message.
        /// </summary>
        public TimeSpan MaxTime { get; private set; }

        /// <summary>
        /// Initializes the constraint with a max time.
        /// </summary>
        public DiscardIfNotReceivedBefore(TimeSpan maxTime)
        {
            MaxTime = maxTime;
        }

        /// <summary>
        /// Serializes the constraint into the passed dictionary.
        /// </summary>
        /// <param name="options">Dictionary where to store the data.</param>
        public override void Serialize(Dictionary<string, string> options)
        {
            options["TimeToBeReceived"] = MaxTime.ToString();
        }
    }
}