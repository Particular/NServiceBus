namespace NServiceBus.Transport
{
    using System;
    using System.Collections.Generic;
    using DelayedDelivery;
    using Performance.TimeToBeReceived;

    /// <summary>
    /// Describes additional properties for an outgoing message.
    /// </summary>
    public class DispatchProperties : Dictionary<string, string>
    {
        //These can't be changed to be backwards compatible with previous versions of the core
        internal static string DoNotDeliverBeforeKeyName = "DeliverAt";
        internal static string DelayDeliveryWithKeyName = "DelayDeliveryFor";
        static string DiscardIfNotReceivedBeforeKeyName = "TimeToBeReceived";

        /// <summary>
        /// Creates a new instance of <see cref="DispatchProperties"/>.
        /// </summary>
        public DispatchProperties()
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="DispatchProperties"/> an copies the values from the provided dictionary.
        /// </summary>
        public DispatchProperties(Dictionary<string, string> properties) : base(properties ?? new Dictionary<string, string>())
        {
        }

        /// <summary>
        /// Delay message delivery to a specific <see cref="DateTimeOffset"/>.
        /// </summary>
        public DoNotDeliverBefore DoNotDeliverBefore
        {
            get => ContainsKey(DoNotDeliverBeforeKeyName)
                ? new DoNotDeliverBefore(DateTimeOffsetHelper.ToDateTimeOffset(this[DoNotDeliverBeforeKeyName]))
                : null;

            set => this[DoNotDeliverBeforeKeyName] = DateTimeOffsetHelper.ToWireFormattedString(value.At);
        }

        /// <summary>
        /// Delay message delivery by a certain <see cref="TimeSpan"/>.
        /// </summary>
        public DelayDeliveryWith DelayDeliveryWith
        {
            get => ContainsKey(DelayDeliveryWithKeyName)
                ? new DelayDeliveryWith(TimeSpan.Parse(this[DelayDeliveryWithKeyName]))
                : null;

            set => this[DelayDeliveryWithKeyName] = value.Delay.ToString();
        }

        /// <summary>
        /// Discard the message after a certain period of time.
        /// </summary>
        public DiscardIfNotReceivedBefore DiscardIfNotReceivedBefore
        {
            get => ContainsKey(DiscardIfNotReceivedBeforeKeyName)
                ? new DiscardIfNotReceivedBefore(TimeSpan.Parse(this[DiscardIfNotReceivedBeforeKeyName]))
                : null;

            set => this[DiscardIfNotReceivedBeforeKeyName] = value.MaxTime.ToString();
        }
    }
}
