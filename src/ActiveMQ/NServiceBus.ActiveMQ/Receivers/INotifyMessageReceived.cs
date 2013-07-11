namespace NServiceBus.Transports.ActiveMQ.Receivers
{
    using System;
    using NServiceBus.Unicast.Transport.Transactional;
    using Unicast.Transport;

    public interface INotifyMessageReceived : IDisposable
    {
        void Start(Address address, TransactionSettings settings);
        void Stop();
    }
}