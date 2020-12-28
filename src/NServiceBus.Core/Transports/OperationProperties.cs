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

        /// <summary>
        /// 
        /// </summary>
        public Dictionary<string, string> Properties { get; }

        /// <summary>
        /// 
        /// </summary>
        public OperationProperties() : this(new Dictionary<string, string>())
        {
        }

        /// <summary>
        /// 
        /// </summary>
        public OperationProperties(Dictionary<string, string> properties)
        {
            Properties = properties;
        }

        /// <summary>
        /// 
        /// </summary>
        public DoNotDeliverBefore DoNotDeliverBefore
        {
            get => Properties.ContainsKey(DoNotDeliverBeforeKeyName)
                ? new DoNotDeliverBefore(DateTimeOffsetHelper.ToDateTimeOffset(Properties[DoNotDeliverBeforeKeyName]))
                : null;

            set => Properties[DoNotDeliverBeforeKeyName] = DateTimeOffsetHelper.ToWireFormattedString(value.At);
        }

        /// <summary>
        /// 
        /// </summary>
        public DelayDeliveryWith DelayDeliveryWith
        {
            get => Properties.ContainsKey(DelayDeliveryWithKeyName)
                ? new DelayDeliveryWith(TimeSpan.Parse(Properties[DelayDeliveryWithKeyName]))
                : null;

            set => Properties[DelayDeliveryWithKeyName] = value.Delay.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        public DiscardIfNotReceivedBefore DiscardIfNotReceivedBefore
        {
            get => Properties.ContainsKey(DiscardIfNotReceivedBeforeKeyName)
                ? new DiscardIfNotReceivedBefore(TimeSpan.Parse(Properties[DiscardIfNotReceivedBeforeKeyName]))
                : null;

            set => Properties[DiscardIfNotReceivedBeforeKeyName] = value.MaxTime.ToString();
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
        public static OperationProperties GetTransportProperties(this ContextBag bag)
        {
            Guard.AgainstNull(nameof(bag), bag);

            if (bag.TryGet(out OperationProperties properties))
            {
                return properties;
            }

            return new OperationProperties(new Dictionary<string, string>());
        }

        /// <summary>
        /// Adds a <see cref="OperationProperties" /> to a <see cref="ContextBag" />.
        /// </summary>
        public static void AddTransportProperties(this ContextBag context, OperationProperties properties)
        {
            Guard.AgainstNull(nameof(context), context);
            Guard.AgainstNull(nameof(properties), properties);

            var contextProperties = new OperationProperties(properties.Properties);
            context.Set(contextProperties);
        }

        /// <summary>
        /// 
        /// </summary>
        public static OperationProperties AsTransportProperties(this Dictionary<string, string> properties)
        {
            return new OperationProperties(properties);
        }
    }
}