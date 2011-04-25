namespace NServiceBus.Gateway.Channels
{
    using System.Collections.Specialized;

    public interface IChannelSender
    {
        void Send(string remoteAddress,string localAddress,NameValueCollection headers,byte[] body);
    }
}