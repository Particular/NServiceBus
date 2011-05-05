namespace NServiceBus.Gateway.Channels
{
    using System.Collections.Specialized;

    public interface IChannelSender
    {
        ChannelType Type { get; }
  
        void Send(string remoteAddress,NameValueCollection headers,byte[] body);
    }
}