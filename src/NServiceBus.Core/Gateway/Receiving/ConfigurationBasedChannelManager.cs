namespace NServiceBus.Gateway.Receiving
{
    using System.Collections.Generic;
    using System.Linq;
    using Channels;
    using Config;

    public class ConfigurationBasedChannelManager : IManageReceiveChannels
    {
        public ConfigurationBasedChannelManager()
        {
            channels = Configure.GetConfigSection<GatewayConfig>().GetChannels();

        }

        public IEnumerable<ReceiveChannel> GetReceiveChannels()
        {
            return channels;
        }

        public Channel GetDefaultChannel(IEnumerable<string> types)
        {
            var channelsFortypes = channels.Where(c => types.Contains(c.Type)).ToList();
            var defaultChannel = channelsFortypes.SingleOrDefault(c => c.Default) ?? channelsFortypes.First();

            return defaultChannel;
        }

        readonly IEnumerable<ReceiveChannel> channels;
    }
}
