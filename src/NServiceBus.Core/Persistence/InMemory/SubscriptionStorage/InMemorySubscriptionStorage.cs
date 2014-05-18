namespace NServiceBus.InMemory.SubscriptionStorage
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using Unicast.Subscriptions;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    /// <summary>
    /// In memory implementation of the subscription storage
    /// </summary>
    class InMemorySubscriptionStorage : ISubscriptionStorage
    {
        void ISubscriptionStorage.Subscribe(Address address, IEnumerable<MessageType> messageTypes)
        {
            messageTypes.ToList().ForEach(m =>
            {
                List<Address> list;
                if (!storage.TryGetValue(m, out list))
                {
                  storage[m] = list = new List<Address>();
                }

                if (!list.Contains(address))
                {
                    list.Add(address);
                }
            });
        }

        void ISubscriptionStorage.Unsubscribe(Address address, IEnumerable<MessageType> messageTypes)
        {
            messageTypes.ToList().ForEach(m =>
            {
                List<Address> list;
                if (storage.TryGetValue(m, out list))
                {
                    list.Remove(address);
                }
            });
        }


        IEnumerable<Address> ISubscriptionStorage.GetSubscriberAddressesForMessage(IEnumerable<MessageType> messageTypes)
        {
            var result = new List<Address>();
            messageTypes.ToList().ForEach(m =>
            {
                List<Address> list;
                if (storage.TryGetValue(m, out list))
                {
                    result.AddRange(list);
                }
            });

            return result.Distinct();
        }

        public void Init()
        {
        }

        readonly ConcurrentDictionary<MessageType, List<Address>> storage = new ConcurrentDictionary<MessageType, List<Address>>();
    }
}