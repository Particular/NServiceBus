namespace NServiceBus
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Extensibility;
    using Pipeline;
    using Routing;
    using Unicast.Messages;
    using Unicast.Subscriptions;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    class UnicastPublishRouter : IUnicastPublishRouter
    {
        public UnicastPublishRouter(MessageMetadataRegistry messageMetadataRegistry, Func<EndpointInstance, string> transportAddressTranslation, ISubscriptionStorage subscriptionStorage, TimeSpan? cacheFor)
        {
            this.messageMetadataRegistry = messageMetadataRegistry;
            this.transportAddressTranslation = transportAddressTranslation;
            this.subscriptionStorage = subscriptionStorage;
            this.cacheFor = cacheFor;
        }

        public async Task<IEnumerable<UnicastRoutingStrategy>> Route(Type messageType, IDistributionPolicy distributionPolicy, IOutgoingPublishContext publishContext)
        {
            var typesToRoute = messageMetadataRegistry.GetMessageMetadata(messageType).MessageHierarchy;

            var subscribers = await GetSubscribers(publishContext, typesToRoute).ConfigureAwait(false);

            return SelectDestinationsForEachEndpoint(publishContext, distributionPolicy, subscribers);
        }

        Dictionary<string, UnicastRoutingStrategy>.ValueCollection SelectDestinationsForEachEndpoint(IOutgoingPublishContext publishContext, IDistributionPolicy distributionPolicy, IEnumerable<Subscriber> subscribers)
        {
            //Make sure we are sending only one to each transport destination. Might happen when there are multiple routing information sources.
            var addresses = new Dictionary<string, UnicastRoutingStrategy>();
            Dictionary<string, List<string>> groups = null;
            foreach (var subscriber in subscribers)
            {
                if(subscriber.Endpoint == null)
                {
                    if (!addresses.ContainsKey(subscriber.TransportAddress))
                    {
                        addresses.Add(subscriber.TransportAddress, new UnicastRoutingStrategy(subscriber.TransportAddress));
                    }

                    continue;
                }

                groups = groups ?? new Dictionary<string, List<string>>();

                List<string> transportAddresses;
                if (groups.TryGetValue(subscriber.Endpoint, out transportAddresses))
                {
                    transportAddresses.Add(subscriber.TransportAddress);
                }
                else
                {
                    groups[subscriber.Endpoint] = new List<string> { subscriber.TransportAddress };
                }
            }

            if (groups != null)
            {
                foreach (var group in groups)
                {
                    var instances = group.Value.ToArray(); // could we avoid this?
                    var distributionContext = new DistributionContext(instances, publishContext.Message, publishContext.MessageId, publishContext.Headers, transportAddressTranslation, publishContext.Extensions);
                    var subscriber = distributionPolicy.GetDistributionStrategy(group.Key, DistributionStrategyScope.Publish).SelectDestination(distributionContext);

                    if (!addresses.ContainsKey(subscriber))
                    {
                        addresses.Add(subscriber, new UnicastRoutingStrategy(subscriber));
                    }
                }
            }

            return addresses.Values;
        }

        async Task<IEnumerable<Subscriber>> GetSubscribers(IExtendable publishContext, Type[] typesToRoute)
        {
            var context = publishContext.Extensions;

            if (cacheFor == null)
            {
                return await GetSubscribers(context, typesToRoute)
                    .ConfigureAwait(false);
            }

            var typeNames = typesToRoute.Select(_ => _.FullName);
            var key = string.Join(",", typeNames);
            CacheItem cacheItem;
            if (cache.TryGetValue(key, out cacheItem))
            {
                if (DateTimeOffset.UtcNow - cacheItem.Stored < cacheFor)
                {
                    return cacheItem.Subscribers;
                }
            }

            var baseSubscribers = await GetSubscribers(context, typesToRoute)
                .ConfigureAwait(false);

            cacheItem = new CacheItem
            {
                Stored = DateTimeOffset.UtcNow,
                Subscribers = baseSubscribers.ToArray()
            };

            cache.AddOrUpdate(key, s => cacheItem, (s, tuple) => cacheItem);

            return cacheItem.Subscribers;
        }

        Task<IEnumerable<Subscriber>> GetSubscribers(ContextBag context, Type[] typesToRoute)
        {
            var messageTypes = typesToRoute.Select(t => new MessageType(t));
            return subscriptionStorage.GetSubscriberAddressesForMessage(messageTypes, context);
        }

        MessageMetadataRegistry messageMetadataRegistry;
        Func<EndpointInstance, string> transportAddressTranslation;
        ISubscriptionStorage subscriptionStorage;
        TimeSpan? cacheFor;
        ConcurrentDictionary<string, CacheItem> cache = new ConcurrentDictionary<string, CacheItem>();

        class CacheItem
        {
            public DateTimeOffset Stored;
            public Subscriber[] Subscribers;
        }
    }
}