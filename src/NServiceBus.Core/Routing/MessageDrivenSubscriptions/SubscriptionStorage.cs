namespace NServiceBus
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Extensibility;
    using Unicast.Subscriptions;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    class SubscriptionStorage
    {
        ISubscriptionStorage subscriptionStorage;

        public SubscriptionStorage(ISubscriptionStorage subscriptionStorage, TimeSpan? cacheFor)
        {
            this.subscriptionStorage = subscriptionStorage;
            this.cacheFor = cacheFor;
            if (cacheFor != null)
            {
                cache = new ConcurrentDictionary<string, CacheItem>();
            }
        }
        public async Task Subscribe(Subscriber subscriber, MessageType messageType, ContextBag contextExtensions)
        {
            await subscriptionStorage.Subscribe(subscriber, messageType, contextExtensions)
                .ConfigureAwait(false);
            if (cacheFor != null)
            {
                cache.Clear();
            }
        }

        public async Task Unsubscribe(Subscriber subscriber, MessageType messageType, ContextBag contextExtensions)
        {
            await subscriptionStorage.Unsubscribe(subscriber, messageType, contextExtensions).
                ConfigureAwait(false);
            if (cacheFor != null)
            {
                cache.Clear();
            }
        }

        public async Task<IEnumerable<Subscriber>> GetSubscribers(IEnumerable<MessageType> messageTypes, ContextBag context)
        {
            if (cacheFor == null)
            {
                return await subscriptionStorage.GetSubscriberAddressesForMessage(messageTypes, context)
                    .ConfigureAwait(false);
            }

            var messageTypesList = messageTypes.ToList();
            var typeNames = messageTypesList.Select(_ => _.TypeName);
            var key = string.Join(",", typeNames);
            CacheItem cacheItem;
            if (cache.TryGetValue(key, out cacheItem))
            {
                if (DateTimeOffset.UtcNow - cacheItem.Stored < cacheFor)
                {
                    return cacheItem.Subscribers;
                }
            }

            var baseSubscribers = await subscriptionStorage.GetSubscriberAddressesForMessage(messageTypesList, context)
                .ConfigureAwait(false);

            cacheItem = new CacheItem
            {
                Stored = DateTimeOffset.UtcNow,
                Subscribers = baseSubscribers.ToArray()
            };

            cache.AddOrUpdate(key, s => cacheItem, (s, tuple) => cacheItem);

            return cacheItem.Subscribers;
        }

        TimeSpan? cacheFor;
        ConcurrentDictionary<string, CacheItem> cache;

        class CacheItem
        {
            public DateTimeOffset Stored;
            public Subscriber[] Subscribers;
        }

    }
}