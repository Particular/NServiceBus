namespace NServiceBus.Gateway.Routing.Routers
{
    using System;
    using System.Collections.Generic;
    using Channels.Http;
    using Unicast.Transport;

    public class KeyPrefixConventionMessageRouter : IRouteMessages
    {
        public IEnumerable<Site> GetDestinationSitesFor(TransportMessage messageToDispatch)
        {
            if (!messageToDispatch.Headers.ContainsKey(Headers.DestinationSites))
                throw new InvalidOperationException("Header not found " + Headers.DestinationSites);
                
            var siteKeys = messageToDispatch.Headers[Headers.DestinationSites].Split(',');

            foreach (var siteKey in siteKeys)
            {
                if(siteKey.StartsWith("http://"))
                    yield return new Site
                                     {
                                         Address = siteKey,
                                         ChannelType = typeof (HttpChannelSender),
                                         Key = siteKey
                                     };
           
            }
                   
        }
    }
}