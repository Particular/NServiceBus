namespace NServiceBus.Unicast.Queuing.ActiveMQ
{
    using System;

    using NServiceBus.Unicast.Transport;

    public interface INotifyMessageReceived
    {
        event EventHandler<TransportMessageReceivedEventArgs> MessageReceived;
        void Init(Address address, bool transactional);
    }
}