namespace NServiceBus.Gateway.Channels
{
    using Unicast.Transport;

    public interface IChannelSender
    {
        void Send(TransportMessage msg, string remoteAddress);
    }
}