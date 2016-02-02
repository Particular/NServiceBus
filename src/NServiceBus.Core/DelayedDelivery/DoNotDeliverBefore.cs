namespace NServiceBus.DelayedDelivery
{
    using System;

    /// <summary>
    /// Represent a constraint that the message can't be made available for consumption before a given time.
    /// </summary>
    public class DoNotDeliverBefore : DelayedDeliveryConstraint
    {
        /// <summary>
        /// Initializes a new insatnce of <see cref="DoNotDeliverBefore" />.
        /// </summary>
        /// <param name="at">The earliest time this message should be made available to its consumers.</param>
        public DoNotDeliverBefore(DateTime at)
        {
            if (at.ToUniversalTime() <= DateTime.UtcNow)
            {
                throw new ArgumentException("Delivery time must be in the future", nameof(at));
            }

            At = at;
        }

        /// <summary>
        /// The actual time when the message can be available to the recipient.
        /// </summary>
        public DateTime At { get; }
    }
}