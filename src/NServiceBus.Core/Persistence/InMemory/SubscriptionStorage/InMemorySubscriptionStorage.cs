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
        void ISubscriptionStorage.Subscribe(Address address, IEnumerable<MessageType> messageTypes)
        {
            foreach (var m in messageTypes)
            {
                var dict = storage.GetOrAdd(m, type => new ConcurrentDictionary<Address, object>());

                dict.AddOrUpdate(address, addValueFactory, updateValueFactory);
            }
        }

        void ISubscriptionStorage.Unsubscribe(Address address, IEnumerable<MessageType> messageTypes)
        {
            foreach (var m in messageTypes)
            {
                ConcurrentDictionary<Address, object> dict;
                if (storage.TryGetValue(m, out dict))
                {
                    object _;
                    dict.TryRemove(address, out _);
                }
            }
        }

        IEnumerable<Address> ISubscriptionStorage.GetSubscriberAddressesForMessage(IEnumerable<MessageType> messageTypes)
        {
            var result = new HashSet<Address>();
            foreach (var m in messageTypes)
            {
                ConcurrentDictionary<Address, object> list;
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

        readonly ConcurrentDictionary<MessageType, ConcurrentDictionary<Address, object>> storage = new ConcurrentDictionary<MessageType, ConcurrentDictionary<Address, object>>();
        Func<Address, object> addValueFactory = a => null;
        Func<Address, object, object> updateValueFactory = (a, o) => null;
    }
}