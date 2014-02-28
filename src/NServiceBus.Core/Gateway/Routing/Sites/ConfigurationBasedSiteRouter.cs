namespace NServiceBus.Gateway.Routing.Sites
{
    using System.Collections.Generic;
    using Config;

    public class ConfigurationBasedSiteRouter : IRouteMessagesToSites
    {
        public ConfigurationBasedSiteRouter()
        {
            var section = Configure.GetConfigSection<GatewayConfig>();
            if (section != null)
            {
                sites = section.SitesAsDictionary();
            }
        }

        public IEnumerable<Site> GetDestinationSitesFor(TransportMessage messageToDispatch)
        {
            string destinationSites;
            if (messageToDispatch.Headers.TryGetValue(Headers.DestinationSites, out destinationSites))
            {
                var siteKeys = destinationSites.Split(',');

                foreach (var siteKey in siteKeys)
                {
                    Site site;
                    if (sites.TryGetValue(siteKey, out site))
                    {
                        yield return site;
                    }
                }
            }
        }

        readonly IDictionary<string, Site> sites = new Dictionary<string, Site>();
    }
}
