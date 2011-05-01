namespace NServiceBus.Gateway.Notifications
{
    using System;
    using Unicast.Transport;

    public interface IMessageNotifier : INotifyAboutMessages
    {
        void RaiseMessageForwarded(Type fromChannel,Type toChannel, TransportMessage message);
    }
}