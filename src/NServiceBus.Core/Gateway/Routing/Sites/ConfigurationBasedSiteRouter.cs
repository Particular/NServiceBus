namespace NServiceBus.Gateway.Routing.Sites
{
    using System.Collections.Generic;
    using Config;

    public class ConfigurationBasedSiteRouter : IRouteMessagesToSites
    {
        public ConfigurationBasedSiteRouter()
        {
            var section = Configure.ConfigurationSource.GetConfiguration<GatewayConfig>();
            if (section != null)
            {
                sites = section.SitesAsDictionary();
            }
        }

        public IEnumerable<Site> GetDestinationSitesFor(TransportMessage messageToDispatch)
        {
            if (messageToDispatch.Headers.ContainsKey(Headers.DestinationSites))
            {
                var siteKeys = messageToDispatch.Headers[Headers.DestinationSites].Split(',');


                foreach (var siteKey in siteKeys)
                {
                    if (sites.ContainsKey(siteKey))
                    {
                        yield return sites[siteKey];
                    }
                }
            }
        }

        readonly IDictionary<string, Site> sites = new Dictionary<string, Site>();
    }
}