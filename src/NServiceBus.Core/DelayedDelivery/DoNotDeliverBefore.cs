namespace NServiceBus.DelayedDelivery
{
    using System;
    using System.Collections.Generic;
    using DeliveryConstraints;

    /// <summary>
    /// Represent a constraint that the message can't be made available for consumption before a given time.
    /// </summary>
    public class DoNotDeliverBefore : DelayedDeliveryConstraint
    {
        /// <summary>
        /// Initializes a new instance of <see cref="DoNotDeliverBefore" />.
        /// </summary>
        /// <param name="at">The earliest time this message should be made available to its consumers.</param>
        public DoNotDeliverBefore(DateTime at)
        {
            At = at;
        }

        static  DoNotDeliverBefore()
        {
            RegisterDeserializer(Deserialize);
        }

        /// <summary>
        /// The actual time when the message can be available to the recipient.
        /// </summary>
        public DateTime At { get; }

        /// <inheritdoc/>
        protected override void Serialize(Dictionary<string, string> options)
        {
            options["DeliverAt"] = DateTimeExtensions.ToWireFormattedString(At);
        }

        static void Deserialize(IReadOnlyDictionary<string, string> options, ICollection<DeliveryConstraint> constraints)
        {
            if (options.TryGetValue("DeliverAt", out var deliverAt))
            {
                constraints.Add(new DoNotDeliverBefore(DateTimeExtensions.ToUtcDateTime(deliverAt)));
            }
        }
    }
}