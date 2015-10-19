namespace NServiceBus.InMemory.SubscriptionStorage
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Unicast.Subscriptions;
    using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;

    class InMemorySubscriptionStorage : ISubscriptionStorage
    {
        public Task Subscribe(Subscriber subscriber, IEnumerable<MessageType> messageTypes, ContextBag context)
        {
            foreach (var m in messageTypes)
            {
                var dict = storage.GetOrAdd(m, type => new ConcurrentDictionary<string, Subscriber>(StringComparer.OrdinalIgnoreCase));

                dict.AddOrUpdate(subscriber.TransportAddress, _ => subscriber, (_, __) => subscriber);
            }
            return TaskEx.Completed;
        }

        public Task Unsubscribe(Subscriber subscriber, IEnumerable<MessageType> messageTypes, ContextBag context)
        {
            foreach (var m in messageTypes)
            {
                ConcurrentDictionary<string, Subscriber> dict;
                if (storage.TryGetValue(m, out dict))
                {
                    Subscriber _;
                    dict.TryRemove(subscriber.TransportAddress, out _);
                }
            }
            return TaskEx.Completed;
        }

        public Task<IEnumerable<Subscriber>> GetSubscriberAddressesForMessage(IEnumerable<MessageType> messageTypes, ContextBag context)
        {
            var result = new HashSet<Subscriber>();
            foreach (var m in messageTypes)
            {
                ConcurrentDictionary<string, Subscriber> list;
                if (storage.TryGetValue(m, out list))
                {
                    result.UnionWith(list.Values);
                }
            }
            return Task.FromResult((IEnumerable<Subscriber>) result);
        }

        ConcurrentDictionary<MessageType, ConcurrentDictionary<string, Subscriber>> storage = new ConcurrentDictionary<MessageType, ConcurrentDictionary<string, Subscriber>>();
    }
}