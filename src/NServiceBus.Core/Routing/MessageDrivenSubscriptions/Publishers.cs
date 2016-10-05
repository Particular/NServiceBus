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
        /// Adds or replaces a set of publisher registrations. The registration set is identified <paramref name="sourceKey"></paramref>.
        /// If the method is called the first time with a given <paramref name="sourceKey"></paramref>, the registrations are added.
        /// If the method is called with the same <paramref name="sourceKey"></paramref> multiple times, the publishers registered previously under this key are replaced.
        /// </summary>
        /// <param name="sourceKey">Key for this registration source.</param>
        /// <param name="entries">Entries.</param>
        public void AddOrReplacePublishers(string sourceKey, IList<PublisherTableEntry> entries)
        {
            lock (updateLock)
            {
                publisherRegistrations[sourceKey] = entries;
                var newRouteTable = new Dictionary<Type, HashSet<PublisherAddress>>();
                foreach (var entry in publisherRegistrations.Values.SelectMany(g => g))
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
        Dictionary<object, IList<PublisherTableEntry>> publisherRegistrations = new Dictionary<object, IList<PublisherTableEntry>>();
        object updateLock = new object();
    }
}
