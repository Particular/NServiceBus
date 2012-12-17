namespace NServiceBus.ActiveMQ
{
    using System;

    using NServiceBus.Unicast.Transport;
    using NServiceBus.Unicast.Transport.Transactional;

    public interface INotifyMessageReceived : IDisposable
    {
        event EventHandler<TransportMessageReceivedEventArgs> MessageReceived;
        void Start(Address address, TransactionSettings settings);
    }
}