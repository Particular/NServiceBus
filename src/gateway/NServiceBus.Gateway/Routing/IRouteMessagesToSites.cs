namespace NServiceBus.Gateway.Routing
{
    using System.Collections.Generic;
    using Unicast.Transport;

    public interface IRouteMessagesToSites

    {
        IEnumerable<Site> GetDestinationSitesFor(TransportMessage messageToDispatch);
    }
}