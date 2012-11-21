namespace NServiceBus.Unicast.Queuing.ActiveMQ
{
    using System;

    using NServiceBus.Unicast.Transport;

    public interface INotifyMessageReceived : IDisposable
    {
        event EventHandler<TransportMessageReceivedEventArgs> MessageReceived;
        void Start(Address address);
    }
}