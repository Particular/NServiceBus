namespace NServiceBus.InMemory.SubscriptionStorage
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using NServiceBus.Unicast.Subscriptions;
    using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;

    /// <summary>
    ///     In memory implementation of the subscription storage
    /// </summary>
    class InMemorySubscriptionStorage : ISubscriptionStorage
    {
        public void Subscribe(string address, IEnumerable<MessageType> messageTypes)
        {
            foreach (var m in messageTypes)
            {
                var dict = storage.GetOrAdd(m, type => new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase));

                dict.AddOrUpdate(address, addValueFactory, updateValueFactory);
            }
        }

        public void Unsubscribe(string address, IEnumerable<MessageType> messageTypes)
        {
            foreach (var m in messageTypes)
            {
                ConcurrentDictionary<string, object> dict;
                if (storage.TryGetValue(m, out dict))
                {
                    object _;
                    dict.TryRemove(address, out _);
                }
            }
        }

        public IEnumerable<string> GetSubscriberAddressesForMessage(IEnumerable<MessageType> messageTypes)
        {
            var result = new HashSet<string>();
            foreach (var m in messageTypes)
            {
                ConcurrentDictionary<string, object> list;
                if (storage.TryGetValue(m, out list))
                {
                    result.UnionWith(list.Keys);
                }
            }
            return result;
        }

        public void Init()
        {
        }

        readonly ConcurrentDictionary<MessageType, ConcurrentDictionary<string, object>> storage = new ConcurrentDictionary<MessageType, ConcurrentDictionary<string, object>>();
        Func<string, object> addValueFactory = a => null;
        Func<string, object, object> updateValueFactory = (a, o) => null;
    }
}