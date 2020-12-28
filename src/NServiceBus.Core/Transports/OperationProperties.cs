using System;
using System.Collections.Generic;
using NServiceBus.DelayedDelivery;
using NServiceBus.Extensibility;
using NServiceBus.Performance.TimeToBeReceived;

namespace NServiceBus.Transports
{
    /// <summary>
    ///
    /// </summary>
    public class OperationProperties
    {
        //These can't be changed to be backwards compatible with previous versions of the core
        static string DoNotDeliverBeforeKeyName = "DeliverAt";
        static string DelayDeliveryWithKeyName = "DelayDeliveryFor";
        static string DiscardIfNotReceivedBeforeKeyName = "TimeToBeReceived";

        private Dictionary<string, string> properties;

        /// <summary>
        ///
        /// </summary>
        public OperationProperties() : this(new Dictionary<string, string>())
        {

        }

        private OperationProperties(Dictionary<string, string> properties)
        {
            this.properties = properties;
        }

        /// <summary>
        ///
        /// </summary>
        public DoNotDeliverBefore DoNotDeliverBefore
        {
            get => properties.ContainsKey(DoNotDeliverBeforeKeyName)
                ? new DoNotDeliverBefore(DateTimeOffsetHelper.ToDateTimeOffset(properties[DoNotDeliverBeforeKeyName]))
                : null;

            set => properties[DoNotDeliverBeforeKeyName] = DateTimeOffsetHelper.ToWireFormattedString(value.At);
        }

        /// <summary>
        ///
        /// </summary>
        public DelayDeliveryWith DelayDeliveryWith
        {
            get => properties.ContainsKey(DelayDeliveryWithKeyName)
                ? new DelayDeliveryWith(TimeSpan.Parse(properties[DelayDeliveryWithKeyName]))
                : null;

            set => properties[DelayDeliveryWithKeyName] = value.Delay.ToString();
        }

        /// <summary>
        ///
        /// </summary>
        public DiscardIfNotReceivedBefore DiscardIfNotReceivedBefore
        {
            get => properties.ContainsKey(DiscardIfNotReceivedBeforeKeyName)
                ? new DiscardIfNotReceivedBefore(TimeSpan.Parse(properties[DiscardIfNotReceivedBeforeKeyName]))
                : null;

            set => properties[DiscardIfNotReceivedBeforeKeyName] = value.MaxTime.ToString();
        }

        /// <summary>
        /// Converts this instance into a serializable dictionary
        /// </summary>
        public Dictionary<string, string> ToDictionary()
        {
            return properties;
        }

        /// <summary>
        /// Creates an OperationProperties from the supplied dictionary.
        /// </summary>
        /// <param name="dictionary">The dictionary to deserialize values from.</param>
        public static OperationProperties FromDictionary(Dictionary<string, string> dictionary)
        {
            return new OperationProperties()
            {
                properties = dictionary
            };
        }
    }

    /// <summary>
    ///
    /// </summary>
    public static class ContextBagExtensions
    {
        /// <summary>
        ///
        /// </summary>
        public static OperationProperties GetOperationProperties(this ContextBag bag)
        {
            Guard.AgainstNull(nameof(bag), bag);

            if (bag.TryGet(out OperationProperties properties))
            {
                return properties;
            }

            return new OperationProperties();
        }

        /// <summary>
        /// Adds a <see cref="OperationProperties" /> to a <see cref="ContextBag" />.
        /// </summary>
        public static void AddOperationProperties(this ContextBag context, OperationProperties properties)
        {
            Guard.AgainstNull(nameof(context), context);
            Guard.AgainstNull(nameof(properties), properties);

            //TODO what's the point of copying over the dictionary? It's still a reference.
            var contextProperties = OperationProperties.FromDictionary(properties.ToDictionary());
            context.Set(contextProperties);
        }

        /// <summary>
        ///
        /// </summary>
        //TODO do we still need this?
        public static OperationProperties AsOperationProperties(this Dictionary<string, string> properties)
        {
            return OperationProperties.FromDictionary(properties);
        }
    }
}