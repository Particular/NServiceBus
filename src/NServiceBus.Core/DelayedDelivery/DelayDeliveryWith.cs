namespace NServiceBus.DelayedDelivery
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represent a constraint that the message can't be delivered before the specified delay has elapsed
    /// </summary>
    public class DelayDeliveryWith : DelayedDeliveryConstraint
    {
        /// <summary>
        /// Initializes the constraint
        /// </summary>
        /// <param name="delay">How long to delay the delivery of the message</param>
        public DelayDeliveryWith(TimeSpan delay)
        {
            Guard.AgainstNegative(delay,"delay");

            Delay = delay;
        }

        /// <summary>
        /// The requested delay
        /// </summary>
        public TimeSpan Delay { get; private set; }

        /// <summary>
        /// Serializes the constraint into the passed dictionary
        /// </summary>
        /// <param name="options">Dictionary where to store the data</param>
        public override void Serialize(Dictionary<string, string> options)
        {
            options["DelayDeliveryFor"] = Delay.ToString();
        }
    }
}