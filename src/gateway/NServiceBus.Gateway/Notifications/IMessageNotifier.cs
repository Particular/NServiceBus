namespace NServiceBus.Gateway.Notifications
{
    using System;
    using Unicast.Transport;

    public interface IMessageNotifier : INotifyAboutMessages
    {
        void RaiseMessageForwarded(string fromChannel,string toChannel, TransportMessage message);
    }
}