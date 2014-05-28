namespace NServiceBus.Gateway.Receiving
{
    using System.Collections.Generic;
    using System.Linq;
    using Channels;

    public class ConventionBasedChannelManager : IManageReceiveChannels
    {
        public IEnumerable<ReceiveChannel> GetReceiveChannels()
        {
            yield return new ReceiveChannel
            {
                Address = string.Format("http://localhost/{0}/", EndpointName),
                Type = "Http",
                NumberOfWorkerThreads = 1
            };
        }

        public Channel GetDefaultChannel()
        {
            return GetReceiveChannels().First();
        }

        public string EndpointName { get; set; }
    }
}