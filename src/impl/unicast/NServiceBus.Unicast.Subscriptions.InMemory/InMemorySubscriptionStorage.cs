using System.Collections.Generic;
using System.Linq;

namespace NServiceBus.Unicast.Subscriptions.InMemory
{
    using System.Collections.Concurrent;

    /// <summary>
    /// In memory implementation of the subscription storage
    /// </summary>
    public class InMemorySubscriptionStorage : ISubscriptionStorage
    {
        void ISubscriptionStorage.Subscribe(Address address, IEnumerable<MessageType> messageTypes)
        {
            messageTypes.ToList().ForEach(m =>
            {
                if (!storage.ContainsKey(m))
                    storage[m] = new List<Address>();

                if (!storage[m].Contains(address))
                    storage[m].Add(address);
            });
        }

        void ISubscriptionStorage.Unsubscribe(Address address, IEnumerable<MessageType> messageTypes)
        {
            messageTypes.ToList().ForEach(m =>
            {
                if (storage.ContainsKey(m))
                    storage[m].Remove(address);
            });
        }


        IEnumerable<Address> ISubscriptionStorage.GetSubscriberAddressesForMessage(IEnumerable<MessageType> messageTypes)
        {
            var result = new List<Address>();
            messageTypes.ToList().ForEach(m =>
            {
                if (storage.ContainsKey(m))
                    result.AddRange(storage[m]);
            });

            return result.Distinct();
        }

        public void Init()
        {
        }

        readonly ConcurrentDictionary<MessageType, List<Address>> storage = new ConcurrentDictionary<MessageType, List<Address>>();
    }
}