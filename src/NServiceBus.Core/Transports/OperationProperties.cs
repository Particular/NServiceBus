using System;
using System.Collections.Generic;
using NServiceBus.DelayedDelivery;
using NServiceBus.Performance.TimeToBeReceived;

namespace NServiceBus.Transport
{
    /// <summary>
    /// Describes additional properties for an outgoing message.
    /// </summary>
    public class OperationProperties : Dictionary<string, string>
    {
        //These can't be changed to be backwards compatible with previous versions of the core
        static string DoNotDeliverBeforeKeyName = "DeliverAt";
        static string DelayDeliveryWithKeyName = "DelayDeliveryFor";
        static string DiscardIfNotReceivedBeforeKeyName = "TimeToBeReceived";

        /// <summary>
        /// Creates a new instance of <see cref="OperationProperties"/>.
        /// </summary>
        public OperationProperties()
        {
        }

        /// <summary>
        /// Creates an OperationProperties from the supplied dictionary.
        /// </summary>
        public OperationProperties(Dictionary<string, string> properties) : base(properties)
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="OperationProperties"/>.
        /// </summary>
        public DoNotDeliverBefore DoNotDeliverBefore
        {
            get => ContainsKey(DoNotDeliverBeforeKeyName)
                ? new DoNotDeliverBefore(DateTimeOffsetHelper.ToDateTimeOffset(this[DoNotDeliverBeforeKeyName]))
                : null;

            set => this[DoNotDeliverBeforeKeyName] = DateTimeOffsetHelper.ToWireFormattedString(value.At);
        }

        /// <summary>
        /// Delayed delivery configuration.
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