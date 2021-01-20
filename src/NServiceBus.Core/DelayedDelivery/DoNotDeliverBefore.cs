namespace NServiceBus.DelayedDelivery
{
    using System;

    /// <summary>
    /// Represent a constraint that the message can't be made available for consumption before a given time.
    /// </summary>
    public class DoNotDeliverBefore
    {
        /// <summary>
        /// Initializes a new instance of <see cref="DoNotDeliverBefore" />.
        /// </summary>
        /// <param name="at">The earliest time this message should be made available to its consumers.</param>
        public DoNotDeliverBefore(DateTimeOffset at)
        {
            At = at;
        }

        /// <summary>
        /// The actual time when the message can be available to the recipient.
        /// </summary>
        public DateTimeOffset At { get; }
    }
}