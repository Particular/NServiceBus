namespace NServiceBus.Gateway.Routing.Sites
{
    using System.Collections.Generic;
    using Channels;

    public class OriginatingSiteHeaderRouter : IRouteMessagesToSites
    {
        public IEnumerable<Site> GetDestinationSitesFor(TransportMessage messageToDispatch)
        {
            if (messageToDispatch.Headers.ContainsKey(Headers.OriginatingSite))
            {
                yield return new Site
                {
                    Channel = Channel.Parse(messageToDispatch.Headers[Headers.OriginatingSite]),
                    Key = "Default reply channel",
                    LegacyMode = messageToDispatch.IsLegacyGatewayMessage()
                };
            }
        }
    }
}