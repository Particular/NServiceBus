namespace NServiceBus.Gateway.Receiving
{
    using System.Collections.Generic;
    using System.Linq;
    using Channels;

    public class ConventionBasedChannelManager : IMangageReceiveChannels
    {
        public IEnumerable<Channel> GetActiveChannels()
        {
            yield return new Channel
                             {
                                 Address = string.Format("http://localhost/nservicebus/gateways/{0}/",Configure.EndpointName),
                                 Type = "Http"
                             };
        }

        public Channel GetDefaultChannel()
        {
            return GetActiveChannels().First();
        }
    }
}