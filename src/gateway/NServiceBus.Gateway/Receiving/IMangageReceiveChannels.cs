namespace NServiceBus.Gateway.Receiving
{
    using System.Collections.Generic;
    using Channels;

    public interface IMangageReceiveChannels
    {
        IEnumerable<Channel> GetActiveChannels();
        Channel GetDefaultChannel();
    }
}