using System;
using System.Collections.Generic;
using System.Globalization;
using NServiceBus.DelayedDelivery;
using NServiceBus.Extensibility;
using NServiceBus.Performance.TimeToBeReceived;

namespace NServiceBus.Transports
{
    class TransportProperties
    {
        static string DoNotDeliverBeforeKeyName = typeof(DoNotDeliverBefore).FullName;
        static string DelayDeliveryWithKeyName = typeof(DelayDeliveryWith).FullName;
        static string DiscardIfNotReceivedBeforeKeyName = typeof(DiscardIfNotReceivedBefore).FullName;

        public Dictionary<string, string> Properties { get; }

        public TransportProperties(Dictionary<string, string> properties)
        {
            Properties = properties;
        }

        public DoNotDeliverBefore DoNotDeliverBefore
        {
            get => Properties.ContainsKey(DoNotDeliverBeforeKeyName) 
                ? new DoNotDeliverBefore(DateTime.Parse(Properties[DoNotDeliverBeforeKeyName])) 
                : null;

            set => Properties[DoNotDeliverBeforeKeyName] = value.At.ToString(CultureInfo.InvariantCulture);
        }

        public DelayDeliveryWith DelayDeliveryWith
        {
            get => Properties.ContainsKey(DelayDeliveryWithKeyName) 
                ? new DelayDeliveryWith(TimeSpan.Parse(Properties[DelayDeliveryWithKeyName])) 
                : null;

            set => Properties[DelayDeliveryWithKeyName] = value.Delay.ToString();
        } 
        
        public DiscardIfNotReceivedBefore DiscardIfNotReceivedBefore
        {
            get => Properties.ContainsKey(DiscardIfNotReceivedBeforeKeyName) 
                ? new DiscardIfNotReceivedBefore(TimeSpan.Parse(Properties[DiscardIfNotReceivedBeforeKeyName])) 
                : null;

            set => Properties[DiscardIfNotReceivedBeforeKeyName] = value.MaxTime.ToString();
        }
    }

    static class ContextBagExtensions
    {
        public static TransportProperties GetTransportProperties(this ContextBag bag)
        {
            return bag.Get<TransportProperties>();
        }

        public static TransportProperties AsTransportProperties(this Dictionary<string, string> properties)
        {
            return new TransportProperties(properties);
        }
    }
}