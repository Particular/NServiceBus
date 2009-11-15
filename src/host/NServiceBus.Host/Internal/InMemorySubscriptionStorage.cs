using System.Collections.Generic;
using System.Linq;
using NServiceBus.Unicast.Subscriptions;

namespace NServiceBus.Host.Internal
{
	//TODO, Andreas: This classed should be moved to src\impl\unicast\InMemorySubscriptionStorage so that we can use it 
	//without having to reference the host in "non host" deployments
    public class InMemorySubscriptionStorage : ISubscriptionStorage
    {
        public void Subscribe(string client, IList<string> messageTypes)
        {
            messageTypes.ToList().ForEach(m =>
            {
                if (!storage.ContainsKey(m))
                    storage[m] = new List<string>();

                if (!storage[m].Contains(client))
                    storage[m].Add(client);
            });
        }

        public void Unsubscribe(string client, IList<string> messageTypes)
        {
            messageTypes.ToList().ForEach(m =>
            {
                if (storage.ContainsKey(m))
                    storage[m].Remove(client);
            });
        }

        public IList<string> GetSubscribersForMessage(IList<string> messageTypes)
        {
            var result = new List<string>();
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

        private readonly Dictionary<string, List<string>> storage = new Dictionary<string, List<string>>();
    }
}
