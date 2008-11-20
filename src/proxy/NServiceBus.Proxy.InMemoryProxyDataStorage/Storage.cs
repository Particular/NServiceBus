using System.Collections.Generic;

namespace NServiceBus.Proxy.InMemoryProxyDataStorage
{
    public class Storage : IProxyDataStorage
    {
        public void Save(ProxyData data)
        {
            store[data.Id] = data;
        }

        public ProxyData GetAndRemove(string id)
        {
            ProxyData result;
            store.TryGetValue(id, out result);

            return result;
        }

        private IDictionary<string, ProxyData> store = new Dictionary<string, ProxyData>();
    }
}
