using System.Collections.Generic;

namespace NServiceBus.Proxy.InMemoryImpl
{
    public class SubscriberStorage : ISubscriberStorage
    {
        public void Store(string subscriber)
        {
            if (!storage.Contains(subscriber))
                storage.Add(subscriber);
        }

        public void Remove(string subscriber)
        {
            storage.Remove(subscriber);
        }

        public IEnumerable<string> GetAllSubscribers()
        {
            return storage;
        }

        private readonly IList<string> storage = new List<string>();
    }
}
