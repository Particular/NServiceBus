﻿namespace NServiceBus.DelayedDelivery
{
    using System;
    using System.Collections.Generic;
    using DeliveryConstraints;

    /// <summary>
    /// Represent a constraint that the message can't be delivered before the specified delay has elapsed.
    /// </summary>
    public class DelayDeliveryWith : DelayedDeliveryConstraint
    {
        /// <summary>
        /// Initializes a new instance of <see cref="DelayDeliveryWith" />.
        /// </summary>
        /// <param name="delay">How long to delay the delivery of the message.</param>
        public DelayDeliveryWith(TimeSpan delay)
        {
            Guard.AgainstNegative(nameof(delay), delay);

            Delay = delay;
        }

        static DelayDeliveryWith()
        {
            RegisterDeserializer(Deserialize);
        }

        /// <summary>
        /// The requested delay.
        /// </summary>
        public TimeSpan Delay { get; }

        /// <inheritdoc/>
        protected override void Serialize(Dictionary<string, string> options)
        {
            options["DelayDeliveryFor"] = Delay.ToString();
        }

        static void Deserialize(IReadOnlyDictionary<string, string> options, ICollection<DeliveryConstraint> constraints)
        {
            if (options.TryGetValue("DelayDeliveryFor", out var delay))
            {
                constraints.Add(new DelayDeliveryWith(TimeSpan.Parse(delay)));
            }
        }
    }
}