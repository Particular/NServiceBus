namespace NServiceBus.Gateway.Notifications
{
    using Channels;
    using NServiceBus.Unicast.Transport;

    public interface IMessageNotifier : INotifyAboutMessages
    {
        void RaiseMessageForwarded(ChannelType from,ChannelType to, TransportMessage message);
    }
}