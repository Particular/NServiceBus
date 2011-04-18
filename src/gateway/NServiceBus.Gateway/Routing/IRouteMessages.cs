namespace NServiceBus.Gateway.Routing
{
    using System.Collections.Generic;
    using NServiceBus.Unicast.Transport;

    public interface IRouteMessages

    {
        IEnumerable<Site> GetDestinationSitesFor(TransportMessage messageToDispatch);
    }
}