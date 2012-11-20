namespace NServiceBus.Gateway.Channels
{
    using System.Collections.Generic;
    using System.IO;

    public interface IChannelSender
    {
        void Send(string remoteAddress,IDictionary<string,string> headers,Stream data);
    }
}