namespace NServiceBus.Gateway.Channels
{
    using System.Collections.Specialized;
    using System.IO;

    public interface IChannelSender
    {
        void Send(string remoteAddress,NameValueCollection headers,Stream data);
    }
}