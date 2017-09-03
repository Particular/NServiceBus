namespace NServiceBus
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Extensibility;
    using Unicast.Subscriptions;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    class InMemorySubscriptionStorage : ISubscriptionStorage
    {
        public Task Subscribe(Subscriber subscriber, MessageType messageType, ContextBag context)
        {
            var dict = storage.GetOrAdd(messageType, type => new ConcurrentDictionary<string, Subscriber>(StringComparer.OrdinalIgnoreCase));

            dict.AddOrUpdate(subscriber.TransportAddress, _ => subscriber, (_, __) => subscriber);
            return TaskEx.CompletedTask;
        }

        public Task Unsubscribe(Subscriber subscriber, MessageType messageType, ContextBag context)
        {
            if (storage.TryGetValue(messageType, out var dict))
            {
                dict.TryRemove(subscriber.TransportAddress, out var _);
            }
            return TaskEx.CompletedTask;
        }

        public Task<IEnumerable<Subscriber>> GetSubscriberAddressesForMessage(IEnumerable<MessageType> messageTypes, ContextBag context)
        {
            var result = new HashSet<Subscriber>();
            foreach (var m in messageTypes)
            {
                if (storage.TryGetValue(m, out var list))
                {
                    result.UnionWith(list.Values);
                }
            }
            return Task.FromResult((IEnumerable<Subscriber>) result);
        }

        ConcurrentDictionary<MessageType, ConcurrentDictionary<string, Subscriber>> storage = new ConcurrentDictionary<MessageType, ConcurrentDictionary<string, Subscriber>>();
    }
}