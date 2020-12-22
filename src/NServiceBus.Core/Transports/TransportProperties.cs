using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NServiceBus.DelayedDelivery;
using NServiceBus.Extensibility;
using NServiceBus.Performance.TimeToBeReceived;

namespace NServiceBus.Transports
{
    /// <summary>
    /// 
    /// </summary>
    public class TransportProperties
    {
        static string DoNotDeliverBeforeKeyName = typeof(DoNotDeliverBefore).FullName;
        static string DelayDeliveryWithKeyName = typeof(DelayDeliveryWith).FullName;
        static string DiscardIfNotReceivedBeforeKeyName = typeof(DiscardIfNotReceivedBefore).FullName;

        /// <summary>
        /// 
        /// </summary>
        public Dictionary<string, string> Properties { get; }

        /// <summary>
        /// 
        /// </summary>
        public TransportProperties() : this(new Dictionary<string, string>())
        {
        }

        /// <summary>
        /// 
        /// </summary>
        public TransportProperties(Dictionary<string, string> properties)
        {
            Properties = properties;
        }

        /// <summary>
        /// 
        /// </summary>
        public DoNotDeliverBefore DoNotDeliverBefore
        {
            get => Properties.ContainsKey(DoNotDeliverBeforeKeyName)
                ? new DoNotDeliverBefore(DateTime.Parse(Properties[DoNotDeliverBeforeKeyName]))
                : null;

            set => Properties[DoNotDeliverBeforeKeyName] = value.At.ToString(CultureInfo.InvariantCulture);
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
        public static TransportProperties GetTransportProperties(this ContextBag bag)
        {
            Guard.AgainstNull(nameof(bag), bag);

            if (bag.TryGet(out TransportProperties properties))
            {
                return properties;
            }

            return new TransportProperties(new Dictionary<string, string>());
        }

        /// <summary>
        /// Adds a <see cref="TransportProperties" /> to a <see cref="ContextBag" />.
        /// </summary>
        public static void AddTransportProperties(this ContextBag context, TransportProperties properties)
        {
            Guard.AgainstNull(nameof(context), context);
            Guard.AgainstNull(nameof(properties), properties);

            var contextProperties = new TransportProperties(properties.Properties);
            context.Set(contextProperties);
        }

        /// <summary>
        /// 
        /// </summary>
        public static TransportProperties AsTransportProperties(this Dictionary<string, string> properties)
        {
            return new TransportProperties(properties);
        }
    }
}