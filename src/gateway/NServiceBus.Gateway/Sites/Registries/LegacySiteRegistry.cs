namespace NServiceBus.Gateway.Sites.Registries
{
    using System.Collections.Generic;
    using System.Configuration;
    using Channels;
    using Unicast.Transport;

    public class LegacySiteRegistry:ISiteRegistry
    {
        readonly string remoteUrl;

        public LegacySiteRegistry()
        {
            remoteUrl = ConfigurationManager.AppSettings["RemoteUrl"];
        }

        public IEnumerable<Site> GetDestinationSitesFor(TransportMessage messageToDispatch)
        {
            var address = GetRemoteAddress(messageToDispatch);

            return new []{new Site
            {
                Address = address,
                ChannelType = ChannelType.Http, //todo - hard channeltypes won't fly if we should allow users to add their own channels
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