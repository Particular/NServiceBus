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
            channels = Configure.ConfigurationSource.GetConfiguration<GatewayConfig>().GetChannels();
        }

        public IEnumerable<ReceiveChannel> GetReceiveChannels()
        {
            return channels;
        }

        public Channel GetDefaultChannel()
        {
            var defaultChannel = channels.Where(c => c.Default).SingleOrDefault();

            if (defaultChannel == null)
            {
                defaultChannel = channels.First();
            }
            return defaultChannel;
        }

        readonly IEnumerable<ReceiveChannel> channels;
    }
}