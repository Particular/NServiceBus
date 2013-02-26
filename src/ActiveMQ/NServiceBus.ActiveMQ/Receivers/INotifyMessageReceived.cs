namespace NServiceBus.Transports.ActiveMQ.Receivers
{
    using System;
    using NServiceBus.Unicast.Transport.Transactional;

    public interface INotifyMessageReceived : IDisposable
    {
        void Start(Address address, TransactionSettings settings);
        void Stop();
    }
}