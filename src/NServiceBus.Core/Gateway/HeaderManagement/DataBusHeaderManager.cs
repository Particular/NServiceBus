namespace NServiceBus.Gateway.HeaderManagement
{
    using System.Collections.Generic;
    using System.Linq;
    using Receiving;

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
            var expectedDatabusProperties =
                input.Where(kv => kv.Key.Contains(HeaderMapper.DATABUS_PREFIX)).ToList();
            if (!expectedDatabusProperties.Any())
            {
                return input;
            }

            lock (headers)
            {
                IDictionary<string, string> collection;
                if (!headers.TryGetValue(clientId, out collection))
                {
                    var message = string.Format("Expected {0} databus properties. None were received. Please resubmit.",expectedDatabusProperties.Count);
                    throw new ChannelException(412,message);
                }

                foreach (var propertyHeader in expectedDatabusProperties)
                {
                    if (!collection.ContainsKey(propertyHeader.Key))
                    {
                        var message = string.Format("Databus property {0} was never received. Please resubmit.",propertyHeader.Key);
                        throw new ChannelException(412,message);
                    }
                    input[propertyHeader.Key] = collection[propertyHeader.Key];
                }

                headers.Remove(clientId);
            }
            return input;
        }

        readonly IDictionary<string, IDictionary<string, string>> headers
            = new Dictionary<string, IDictionary<string, string>>();
    }
}