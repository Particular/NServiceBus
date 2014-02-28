namespace NServiceBus.Gateway.Routing.Sites
{
    using System.Collections.Generic;
    using Channels;

    public class OriginatingSiteHeaderRouter : IRouteMessagesToSites
    {
        public IEnumerable<Site> GetDestinationSitesFor(TransportMessage messageToDispatch)
        {
            string originatingSite;
            if (messageToDispatch.Headers.TryGetValue(Headers.OriginatingSite, out originatingSite))
            {
                yield return new Site
                {
                    Channel = Channel.Parse(originatingSite),
                    Key = "Default reply channel",
                    LegacyMode = messageToDispatch.IsLegacyGatewayMessage()
                };
            }
        }
    }
}