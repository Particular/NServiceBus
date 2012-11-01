namespace NServiceBus.Gateway.Routing
{
    using System.Collections.Generic;

    public interface IRouteMessagesToSites

    {
        IEnumerable<Site> GetDestinationSitesFor(TransportMessage messageToDispatch);
    }
}