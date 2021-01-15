using System;
using System.Collections.Generic;
using NServiceBus.DelayedDelivery;
using NServiceBus.Performance.TimeToBeReceived;

namespace NServiceBus.Transport
{
    /// <summary>
    /// Describes additional properties for an outgoing message.
    /// </summary>
    public class OperationProperties
    {
        //These can't be changed to be backwards compatible with previous versions of the core
        static string DoNotDeliverBeforeKeyName = "DeliverAt";
        static string DelayDeliveryWithKeyName = "DelayDeliveryFor";
        static string DiscardIfNotReceivedBeforeKeyName = "TimeToBeReceived";

        private Dictionary<string, string> properties;

        /// <summary>
        /// Creates a new instance of <see cref="OperationProperties"/>.
        /// </summary>
        public OperationProperties() : this(new Dictionary<string, string>())
        {

        }

        /// <summary>
        /// Creates an OperationProperties from the supplied dictionary.
        /// </summary>
        public OperationProperties(Dictionary<string, string> properties)
        {
            this.properties = properties;
        }

        /// <summary>
        /// Creates a new instance of <see cref="OperationProperties"/>.
        /// </summary>
        public DoNotDeliverBefore DoNotDeliverBefore
        {
            get => properties.ContainsKey(DoNotDeliverBeforeKeyName)
                ? new DoNotDeliverBefore(DateTimeOffsetHelper.ToDateTimeOffset(properties[DoNotDeliverBeforeKeyName]))
                : null;

            set => properties[DoNotDeliverBeforeKeyName] = DateTimeOffsetHelper.ToWireFormattedString(value.At);
        }

        /// <summary>
        /// Delayed delivery configuration.
        /// </summary>
        public DelayDeliveryWith DelayDeliveryWith
        {
            get => properties.ContainsKey(DelayDeliveryWithKeyName)
                ? new DelayDeliveryWith(TimeSpan.Parse(properties[DelayDeliveryWithKeyName]))
                : null;

            set => properties[DelayDeliveryWithKeyName] = value.Delay.ToString();
        }

        /// <summary>
        /// Discard the message after a certain period of time.
        /// </summary>
        public DiscardIfNotReceivedBefore DiscardIfNotReceivedBefore
        {
            get => properties.ContainsKey(DiscardIfNotReceivedBeforeKeyName)
                ? new DiscardIfNotReceivedBefore(TimeSpan.Parse(properties[DiscardIfNotReceivedBeforeKeyName]))
                : null;

            set => properties[DiscardIfNotReceivedBeforeKeyName] = value.MaxTime.ToString();
        }

        /// <summary>
        /// Converts this instance into a serializable dictionary.
        /// </summary>
        public Dictionary<string, string> ToDictionary()
        {
            return properties;
        }
    }
}