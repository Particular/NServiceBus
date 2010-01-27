using System.Collections.Generic;
using System.Linq;
using NServiceBus.Unicast.Subscriptions;

namespace NServiceBus.Host.Internal
{
    /// <summary>
    /// In memory storage of subscriptions
    /// </summary>
    public class InMemorySubscriptionStorage : ISubscriptionStorage
    {
        void ISubscriptionStorage.Subscribe(string client, IList<string> messageTypes)
        {
            messageTypes.ToList().ForEach(m =>
            {
                if (!storage.ContainsKey(m))
                    storage[m] = new List<string>();

                if (!storage[m].Contains(client))
                    storage[m].Add(client);
            });
        }

        void ISubscriptionStorage.Unsubscribe(string client, IList<string> messageTypes)
        {
            messageTypes.ToList().ForEach(m =>
            {
                if (storage.ContainsKey(m))
                    storage[m].Remove(client);
            });
        }

        IList<string> ISubscriptionStorage.GetSubscribersForMessage(IList<string> messageTypes)
        {
            var result = new List<string>();
            messageTypes.ToList().ForEach(m =>
            {
                if (storage.ContainsKey(m))
                    result.AddRange(storage[m]);
            });

            return result;
        }

        void ISubscriptionStorage.Init()
        {
        }

        private readonly Dictionary<string, List<string>> storage = new Dictionary<string, List<string>>();
    }
}
