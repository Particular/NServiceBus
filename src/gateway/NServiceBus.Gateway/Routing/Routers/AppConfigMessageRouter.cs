namespace NServiceBus.Gateway.Routing.Routers
{
    using System;
    using System.Collections.Generic;
    using Unicast.Transport;

    //todo - implement this
    public class AppConfigMessageRouter : IRouteMessagesToSites
    {
        public IEnumerable<Site> GetDestinationSitesFor(TransportMessage messageToDispatch)
        {
            throw new NotImplementedException();
        }
    }


    //if (messageToDispatched.Headers.ContainsKey(DestinationSites))
    //    {
    //        var siteKeys = messageToDispatched.Headers[Headers.DestinationSites].Split(',');

    //        foreach (var siteKey in siteKeys)               
    //            yield return siteRegistry.GetByKey(siteKey);
    //    }
    //    else
    //        yield return siteRegistry.DefaultSite();
    ////if (!configuredSites.ContainsKey(siteKey))
    //  throw new InvalidOperationException("Destination site with key " + siteKey + " not found");
    //IDictionary<string, Site> configuredSites;

}