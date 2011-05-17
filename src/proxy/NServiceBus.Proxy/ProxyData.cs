using System;

namespace NServiceBus.Proxy
{
    [Serializable]
    public class ProxyData
    {
        public string Id;
        public Address ClientAddress;
        public string CorrelationId;
    }
}
