namespace NServiceBus.Routing.MessageDrivenSubscriptions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Manages the information about publishers.
    /// </summary>
    public class Publishers
    {
        internal PublisherAddress GetPublisherFor(Type eventType)
        {
            PublisherAddress address;
            return publishers.TryGetValue(eventType, out address)
                ? address
                : null;
        }

        /// <summary>
        /// Adds or replaces a set of publisher registrations.
        /// </summary>
        /// <param name="sourceKey">Key for this registration source.</param>
        /// <param name="entries">Entries.</param>
        public void AddOrReplacePublishers(object sourceKey, IList<PublisherTableEntry> entries)
        {
            lock (updateLock)
            {
                pubisherRegistrations[sourceKey] = entries;
                var newRouteTableTemplate = new Dictionary<Type, PublisherTableEntry>();
                foreach (var entry in pubisherRegistrations.Values.SelectMany(g => g))
                {
                    PublisherTableEntry existing;
                    if (!newRouteTableTemplate.TryGetValue(entry.EventType, out existing))
                    {
                        newRouteTableTemplate[entry.EventType] = entry;
                    }
                    else
                    {
                        throw new Exception($"Publisher for type {entry.EventType.FullName} already registered.");
                    }
                }
                var newRouteTable = newRouteTableTemplate.ToDictionary(e => e.Key, e => e.Value.Address);
                publishers = newRouteTable;
            }
        }

        Dictionary<Type, PublisherAddress> publishers = new Dictionary<Type, PublisherAddress>();
        Dictionary<object, IList<PublisherTableEntry>> pubisherRegistrations = new Dictionary<object, IList<PublisherTableEntry>>();
        object updateLock = new object();
    }
}
