namespace NServiceBus.Gateway.Routing.Sites
{
    using System.Collections.Generic;
    using Channels;

    public class KeyPrefixConventionSiteRouter : IRouteMessagesToSites
    {
        public IEnumerable<Site> GetDestinationSitesFor(TransportMessage messageToDispatch)
        {
            if (messageToDispatch.Headers.ContainsKey(Headers.DestinationSites))
            {
                var siteKeys = messageToDispatch.Headers[Headers.DestinationSites].Split(',');


                foreach (var siteKey in siteKeys)
                {
                    var parts = siteKey.Split(':');

                    if (parts.Length >= 2)
                    {
                        yield return new Site
                        {
                            Channel = new Channel {Address = siteKey, Type = parts[0]},
                            Key = siteKey
                        };
                    }
                }
            }
        }
    }
}