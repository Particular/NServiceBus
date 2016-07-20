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
            List<PublisherAddress> staticPublishersForType;
            if (!staticPublishers.TryGetValue(eventType, out staticPublishersForType))
            {
                staticPublishersForType = emptyList;
            }

            if (dynamicRules.Count > 0)
            {
                var dynamicAddresses = new List<PublisherAddress>();
                foreach (var rule in dynamicRules)
                {
                    var address = rule(eventType);
                    if (address != null)
                    {
                        dynamicAddresses.Add(address);
                    }
                }

                return staticPublishersForType.Count > 0 ? staticPublishersForType.Concat(dynamicAddresses) : dynamicAddresses;
            }

            return staticPublishersForType;
        }

        /// <summary>
        /// Registers a publisher endpoint for a given event type.
        /// </summary>
        /// <param name="eventType">The event type.</param>
        /// <param name="publisher">The publisher endpoint.</param>
        public void Add(Type eventType, string publisher)
        {
            AddStaticPublisher(eventType, PublisherAddress.CreateFromEndpointName(publisher));
        }

        void AddStaticPublisher(Type eventType, PublisherAddress address)
        {
            List<PublisherAddress> addresses;
            if (staticPublishers.TryGetValue(eventType, out addresses))
            {
                addresses.Add(address);
            }
            else
            {
                staticPublishers.Add(eventType, new List<PublisherAddress>
                {
                    address
                });
            }
        }

        /// <summary>
        /// Registers a publisher address for a given event type.
        /// </summary>
        /// <param name="eventType">The event type.</param>
        /// <param name="publisherAddress">The publisher's physical address.</param>
        public void AddByAddress(Type eventType, string publisherAddress)
        {
            AddStaticPublisher(eventType, PublisherAddress.CreateFromPhysicalAddresses(publisherAddress));
        }

        /// <summary>
        /// Adds a dynamic rule.
        /// </summary>
        /// <param name="dynamicRule">The rule.</param>
        public void AddDynamic(Func<Type, PublisherAddress> dynamicRule)
        {
            dynamicRules.Add(dynamicRule);
        }

        Dictionary<Type, List<PublisherAddress>> staticPublishers = new Dictionary<Type, List<PublisherAddress>>();
        List<Func<Type, PublisherAddress>> dynamicRules = new List<Func<Type, PublisherAddress>>();
        List<PublisherAddress> emptyList = new List<PublisherAddress>(0);
    }
}
