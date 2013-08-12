namespace NServiceBus.Gateway.HeaderManagement
{
    using System;
    using MessageMutator;

    public class GatewayHeaderManager : IMutateTransportMessages, INeedInitialization
    {
        public void MutateIncoming(TransportMessage transportMessage)
        {
            returnInfo = null;

            if (!transportMessage.Headers.ContainsKey(Headers.HttpFrom) && 
                !transportMessage.Headers.ContainsKey(Headers.OriginatingSite))
                return;

            returnInfo = new HttpReturnInfo
            {
                //we preserve the httpfrom to be backwards compatible with NServiceBus 2.X 
                HttpFrom = transportMessage.Headers.ContainsKey(Headers.HttpFrom) ? transportMessage.Headers[Headers.HttpFrom] : null,
                OriginatingSite = transportMessage.Headers.ContainsKey(Headers.OriginatingSite) ? transportMessage.Headers[Headers.OriginatingSite] : null,
                ReplyToAddress = transportMessage.ReplyToAddress
            };
        }

        public void MutateOutgoing(object[] messages, TransportMessage transportMessage)
        {
            if (returnInfo == null)
                return;

            if (string.IsNullOrEmpty(transportMessage.CorrelationId))
                return;

            if (transportMessage.Headers.ContainsKey(Headers.HttpTo) || transportMessage.Headers.ContainsKey(Headers.DestinationSites))
                return;

            transportMessage.Headers[Headers.HttpTo] = returnInfo.HttpFrom;
            transportMessage.Headers[Headers.OriginatingSite] = returnInfo.OriginatingSite;

            if (!transportMessage.Headers.ContainsKey(Headers.RouteTo))
                transportMessage.Headers[Headers.RouteTo] = returnInfo.ReplyToAddress.ToString();
        }

        public void Init()
        {
            Configure.Instance.Configurer.ConfigureComponent<GatewayHeaderManager>(
                DependencyLifecycle.InstancePerCall);
        }

        [ThreadStatic]
        static HttpReturnInfo returnInfo;

        class HttpReturnInfo
        {
            public string HttpFrom { get; set; }
            public string OriginatingSite { get; set; }
            public Address ReplyToAddress { get; set; }
        }
    }
}
