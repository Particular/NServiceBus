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

        public Channel GetDefaultChannel()
        {
            var defaultChannel = channels.SingleOrDefault(c => c.Default);

            if (defaultChannel == null)
            {
                defaultChannel = channels.First();
            }
            return defaultChannel;
        }

        readonly IEnumerable<ReceiveChannel> channels;
    }
}
