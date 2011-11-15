using NServiceBus.Gateway.Utils;

namespace NServiceBus.Gateway.Receiving
{
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using Channels;
    using Channels.Http;

    public class LegacyChannelManager : IMangageReceiveChannels
    {
        public IEnumerable<Channel> GetActiveChannels()
        {
            var listenUrl = ConfigurationManager.AppSettings["ListenUrl"];

            yield return new Channel
                       {
                           Address = listenUrl,
                           Type = ChannelTypes.LookupByUrl(listenUrl)
                       };
        }

        public Channel GetDefaultChannel()
        {
            return GetActiveChannels().First();
        }
    }
}