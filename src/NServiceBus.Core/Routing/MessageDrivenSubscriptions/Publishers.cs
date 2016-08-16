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
        internal IEnumerable<PublisherAddress> GetPublisherFor(Type eventType)
        {
            HashSet<PublisherAddress> addresses;
            return publishers.TryGetValue(eventType, out addresses)
                ? addresses
                : Enumerable.Empty<PublisherAddress>();
        }

        /// <summary>
        /// Adds or replaces a set of publisher registrations.
        /// </summary>
        /// <param name="sourceKey">Key for this registration source.</param>
        /// <param name="entries">Entries.</param>
        public void AddOrReplacePublishers(string sourceKey, IList<PublisherTableEntry> entries)
        {
            lock (updateLock)
            {
                pubisherRegistrations[sourceKey] = entries;
                var newRouteTable = new Dictionary<Type, HashSet<PublisherAddress>>();
                foreach (var entry in pubisherRegistrations.Values.SelectMany(g => g))
                {
                    HashSet<PublisherAddress> publishersOfThisEvent;
                    if (!newRouteTable.TryGetValue(entry.EventType, out publishersOfThisEvent))
                    {
                        publishersOfThisEvent = new HashSet<PublisherAddress>();
                        newRouteTable[entry.EventType] = publishersOfThisEvent;
                    }
                    publishersOfThisEvent.Add(entry.Address);
                }
                publishers = newRouteTable;
            }
        }

        Dictionary<Type, HashSet<PublisherAddress>> publishers = new Dictionary<Type, HashSet<PublisherAddress>>();
        Dictionary<object, IList<PublisherTableEntry>> pubisherRegistrations = new Dictionary<object, IList<PublisherTableEntry>>();
        object updateLock = new object();
    }
}
