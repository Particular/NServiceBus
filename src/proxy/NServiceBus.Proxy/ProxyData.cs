using System;

namespace NServiceBus.Proxy
{
    [Serializable]
    public class ProxyData
    {
        public string Id;
        public string ClientAddress;
        public string CorrelationId;
    }
}
