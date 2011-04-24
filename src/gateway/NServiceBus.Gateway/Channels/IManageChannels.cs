namespace NServiceBus.Gateway.Channels
{
    using System.Collections.Generic;

    public interface IManageChannels
    {
        IEnumerable<Channel> GetActiveChannels();
    }
}