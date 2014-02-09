namespace NServiceBus.Gateway.HeaderManagement
{
    using System;
    using MessageMutator;

    public class GatewayHeaderManager : IMutateTransportMessages, INeedInitialization
    {
        public void MutateIncoming(TransportMessage transportMessage)
        {
            returnInfo = null;

            var headers = transportMessage.Headers;
            if (!headers.ContainsKey(Headers.HttpFrom) &&
                !headers.ContainsKey(Headers.OriginatingSite))
            {
                return;
            }

            string originatingSite;
            headers.TryGetValue(Headers.OriginatingSite, out originatingSite);
            string httpFrom;
            headers.TryGetValue(Headers.HttpFrom, out httpFrom);
            returnInfo = new HttpReturnInfo
            {
                //we preserve the httpFrom to be backwards compatible with NServiceBus 2.X 
                HttpFrom = httpFrom,
                OriginatingSite = originatingSite,
                ReplyToAddress = transportMessage.ReplyToAddress,
                LegacyMode = transportMessage.IsLegacyGatewayMessage()
            };
        }

        public void MutateOutgoing(object[] messages, TransportMessage transportMessage)
        {
            if (returnInfo == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(transportMessage.CorrelationId))
            {
                return;
            }

            if (transportMessage.Headers.ContainsKey(Headers.HttpTo) ||
                transportMessage.Headers.ContainsKey(Headers.DestinationSites))
            {
                return;
            }

            transportMessage.Headers[Headers.HttpTo] = returnInfo.HttpFrom;
            transportMessage.Headers[Headers.OriginatingSite] = returnInfo.OriginatingSite;

            if (!transportMessage.Headers.ContainsKey(Headers.RouteTo))
            {
                transportMessage.Headers[Headers.RouteTo] = returnInfo.ReplyToAddress.ToString();
            }

            // send to be backwards compatible with Gateway 3.X
            transportMessage.Headers[GatewayHeaders.LegacyMode] = returnInfo.LegacyMode.ToString();
        }

        public void Init()
        {
            Configure.Instance.Configurer.ConfigureComponent<GatewayHeaderManager>(
                DependencyLifecycle.InstancePerCall);
        }

        [ThreadStatic] static HttpReturnInfo returnInfo;

        class HttpReturnInfo
        {
            public string HttpFrom { get; set; }
            public string OriginatingSite { get; set; }
            public Address ReplyToAddress { get; set; }
            public bool LegacyMode { get; set; }
        }
    }
}