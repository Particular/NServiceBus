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
        void ISubscriptionStorage.Subscribe(string address, IEnumerable<MessageType> messageTypes)
        {
            messageTypes.ToList().ForEach(m =>
            {
                List<string> list;
                if (!storage.TryGetValue(m, out list))
                {
                    storage[m] = list = new List<string>();
                }

                if (!list.Contains(address))
                {
                    list.Add(address);
                }
            });
        }

        void ISubscriptionStorage.Unsubscribe(string address, IEnumerable<MessageType> messageTypes)
        {
            messageTypes.ToList().ForEach(m =>
            {
                List<string> list;
                if (storage.TryGetValue(m, out list))
                {
                    list.Remove(address);
                }
            });
        }


        IEnumerable<string> ISubscriptionStorage.GetSubscriberAddressesForMessage(IEnumerable<MessageType> messageTypes)
        {
            var result = new List<string>();
            messageTypes.ToList().ForEach(m =>
            {
                List<string> list;
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

        readonly ConcurrentDictionary<MessageType, List<string>> storage = new ConcurrentDictionary<MessageType, List<string>>();
    }
}