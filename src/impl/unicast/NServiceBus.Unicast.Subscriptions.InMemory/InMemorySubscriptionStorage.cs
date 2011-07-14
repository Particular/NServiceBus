using System.Collections.Generic;
using System.Linq;

namespace NServiceBus.Unicast.Subscriptions.InMemory
{
    /// <summary>
    /// Inmemory implementation of the subscription storage
    /// </summary>
    public class InMemorySubscriptionStorage : ISubscriptionStorage
    {
        void ISubscriptionStorage.Subscribe(string client, IEnumerable<string> messageTypes)
        {
            ((ISubscriptionStorage)this).Subscribe(Address.Parse(client), messageTypes);
        }

        void ISubscriptionStorage.Subscribe(Address address, IEnumerable<string> messageTypes)
        {
            messageTypes.ToList().ForEach(m =>
            {
                if (!storage.ContainsKey(m))
                    storage[m] = new List<Address>();

                if (!storage[m].Contains(address))
                    storage[m].Add(address);
            });
        }

        void ISubscriptionStorage.Unsubscribe(string client, IEnumerable<string> messageTypes)
        {
            ((ISubscriptionStorage)this).Unsubscribe(Address.Parse(client), messageTypes);
        }

        void ISubscriptionStorage.Unsubscribe(Address address, IEnumerable<string> messageTypes)
        {
            messageTypes.ToList().ForEach(m =>
            {
                if (storage.ContainsKey(m))
                    storage[m].Remove(address);
            });
        }

        IEnumerable<string> ISubscriptionStorage.GetSubscribersForMessage(IEnumerable<string> messageTypes)
        {
            return ((ISubscriptionStorage) this).GetSubscriberAddressesForMessage(messageTypes)
                .Select(a => a.ToString());
        }

        IEnumerable<Address> ISubscriptionStorage.GetSubscriberAddressesForMessage(IEnumerable<string> messageTypes)
        {
            var result = new List<Address>();
            messageTypes.ToList().ForEach(m =>
            {
                if (storage.ContainsKey(m))
                    result.AddRange(storage[m]);
            });

            return result;
        }

        public void Init()
        {
        }

        private readonly Dictionary<string, List<Address>> storage = new Dictionary<string, List<Address>>();
    }
}