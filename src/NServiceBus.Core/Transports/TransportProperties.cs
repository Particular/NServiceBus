using System;
using System.Collections.Generic;
using System.Globalization;
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
            return bag.Get<TransportProperties>();
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