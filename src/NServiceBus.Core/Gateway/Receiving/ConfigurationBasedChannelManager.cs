namespace NServiceBus.Gateway.Receiving
{
    using System.Collections.Generic;
    using System.Linq;
    using Channels;
    using NServiceBus.Config;

    public class ConfigurationBasedChannelManager : IManageReceiveChannels
    {
        readonly IEnumerable<ReceiveChannel> channels;

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
            var defaultChannel =  channels.Where(c => c.Default).SingleOrDefault();

            if (defaultChannel == null)
                defaultChannel = channels.First();
            return defaultChannel;
        }
    }
}