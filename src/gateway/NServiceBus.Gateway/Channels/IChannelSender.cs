namespace NServiceBus.Gateway.Channels
{
    using System.Collections.Specialized;

    public interface IChannelSender
    {
        void Send(string remoteAddress,NameValueCollection headers,byte[] body);
    }
}