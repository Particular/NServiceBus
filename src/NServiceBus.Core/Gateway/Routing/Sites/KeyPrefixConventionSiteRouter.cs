namespace NServiceBus.Gateway.Routing.Sites
{
    using System.Collections.Generic;
    using Channels;

    public class KeyPrefixConventionSiteRouter : IRouteMessagesToSites
    {
        public IEnumerable<Site> GetDestinationSitesFor(TransportMessage messageToDispatch)
        {
            string sites;
            if (messageToDispatch.Headers.TryGetValue(Headers.DestinationSites, out sites))
            {
                var siteKeys = sites.Split(',');

                foreach (var siteKey in siteKeys)
                {
                    var parts = siteKey.Split(':');

                    if (parts.Length >= 2)
                    {
                        yield return new Site
                        {
                            Channel = new Channel
                                {
                                    Address = siteKey, 
                                    Type = parts[0]
                                },
                            Key = siteKey
                        };
                    }
                }
            }
        }
    }
}