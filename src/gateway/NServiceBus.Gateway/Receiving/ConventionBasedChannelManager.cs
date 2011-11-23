namespace NServiceBus.Gateway.Receiving
{
    using System.Collections.Generic;
    using System.Linq;
    using Channels;

    public class ConventionBasedChannelManager : IMangageReceiveChannels
    {
        public IEnumerable<Channel> GetActiveChannels()
        {
            //todo use the INameThisEndpoint abstraction
            string endpointName = Address.Local.Queue;

            yield return new Channel
                             {
                                 Address = string.Format("http://localhost/nservicebus/gateways/{0}/",endpointName),
                                 Type = "Http"
                             };
        }

        public Channel GetDefaultChannel()
        {
            return GetActiveChannels().First();
        }
    }
}