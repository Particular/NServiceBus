namespace NServiceBus.Gateway.Sites
{
    using System.Collections.Generic;
    using Unicast.Transport;

    public interface ISiteRegistry

    {
        IEnumerable<Site> GetDestinationSitesFor(TransportMessage messageToDispatch);
    }
}