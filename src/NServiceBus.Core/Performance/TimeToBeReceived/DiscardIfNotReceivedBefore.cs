namespace NServiceBus.Performance.TimeToBeReceived
{
    using System;
    using System.Collections.Generic;
    using DeliveryConstraints;

    /// <summary>
    /// Instructs the transport to discard the message if it hasn't been received
    /// within the specified <see cref="TimeSpan"/>.
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

        static DiscardIfNotReceivedBefore()
        {
            RegisterDeserializer(Deserialize);
        }

        /// <summary>
        /// The max time to wait before discarding the message.
        /// </summary>
        public TimeSpan MaxTime { get; }

        /// <inheritdoc/>
        protected override void Serialize(Dictionary<string, string> options)
        {
            options["TimeToBeReceived"] = MaxTime.ToString();
        }

        static void Deserialize(IReadOnlyDictionary<string, string> options, ICollection<DeliveryConstraint> constraints)
        {
            if (options.TryGetValue("TimeToBeReceived", out var ttbr))
            {
                constraints.Add(new DiscardIfNotReceivedBefore(TimeSpan.Parse(ttbr)));
            }
        }
    }
}