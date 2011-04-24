namespace NServiceBus.Gateway.Routing.Routers
{
    using System.Collections.Generic;
    using System.Configuration;
    using Channels.Http;
    using Unicast.Transport;

    public class LegacyMessageRouter:IRouteMessagesToSites
    {
        readonly string remoteUrl;

        public LegacyMessageRouter()
        {
            remoteUrl = ConfigurationManager.AppSettings["RemoteUrl"];
        }

        public IEnumerable<Site> GetDestinationSitesFor(TransportMessage messageToDispatch)
        {
            var address = GetRemoteAddress(messageToDispatch);

            return new []{new Site
            {
                Address = address,
                ChannelType = typeof(HttpChannelSender),
                Key = address
            }};
        }

        string GetRemoteAddress(TransportMessage msg)
        {
            //todo - add a eqivalent header that is channel agnostic?
            if (msg.Headers.ContainsKey(Headers.HttpTo))
                return msg.Headers[Headers.HttpTo];

            return remoteUrl;
        }

    }
}