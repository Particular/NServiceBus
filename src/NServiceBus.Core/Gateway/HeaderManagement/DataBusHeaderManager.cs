namespace NServiceBus.Gateway.HeaderManagement
{
    using System.Collections.Generic;
    using System.Linq;

    public class DataBusHeaderManager
    {
        public void InsertHeader(string clientId, string headerKey, string headerValue)
        {
            lock (headers)
            {
                IDictionary<string, string> collection;
                if (!headers.TryGetValue(clientId, out collection))
                {
                    collection = new Dictionary<string, string>();
                    headers[clientId] = collection;
                }
                collection[headerKey] = headerValue;
            }
        }

        public IDictionary<string, string> Reassemble(string clientId, IDictionary<string, string> input)
        {
            lock (headers)
            {
                IDictionary<string, string> collection;
                if (headers.TryGetValue(clientId, out collection))
                    collection.ToList().ForEach(kv => input[kv.Key] = kv.Value);
            }
            return input;
        }

        readonly IDictionary<string, IDictionary<string, string>> headers
            = new Dictionary<string, IDictionary<string, string>>();
    }
}