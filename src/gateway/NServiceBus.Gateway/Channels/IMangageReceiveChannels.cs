namespace NServiceBus.Gateway.Channels
{
    using System.Collections.Generic;

    public interface IMangageReceiveChannels
    {
        IEnumerable<Channel> GetActiveChannels();
        Channel GetDefaultChannel();
    }
}