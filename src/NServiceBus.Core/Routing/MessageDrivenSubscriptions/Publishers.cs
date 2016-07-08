namespace NServiceBus.Routing.MessageDrivenSubscriptions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

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
        /// Registers a publisher endpoint for all event types in a given assembly.
        /// </summary>
        /// <param name="eventAssembly">The assembly containing the event types.</param>
        /// <param name="publisher">The publisher endpoint.</param>
        public void Add(Assembly eventAssembly, string publisher)
        {
            foreach (var type in eventAssembly.GetTypes())
            {
                AddStaticPublisher(type, PublisherAddress.CreateFromEndpointName(publisher));
            }
        }

        /// <summary>
        /// Registers a publisher endpoint for all event types in a given assembly and namespace.
        /// </summary>
        /// <param name="eventAssembly">The assembly containing the event types.</param>
        /// <param name="eventNamespace">The namespace containing the event types.</param>
        /// <param name="publisher">The publisher endpoint.</param>
        public void Add(Assembly eventAssembly, string eventNamespace, string publisher)
        {
            // empty namespace is null, not string.empty
            eventNamespace = eventNamespace == string.Empty ? null : eventNamespace;

            foreach (var type in eventAssembly.GetTypes().Where(t => t.Namespace == eventNamespace))
            {
                AddStaticPublisher(type, PublisherAddress.CreateFromEndpointName(publisher));
            }
        }

        /// <summary>
        /// Adds a dynamic rule which is invoked on each subscription to determine the address of the publisher.
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
