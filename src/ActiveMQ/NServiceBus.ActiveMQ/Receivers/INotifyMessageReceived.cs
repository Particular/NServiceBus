namespace NServiceBus.Transports.ActiveMQ.Receivers
{
    using System;
    using Unicast.Transport;

    public interface INotifyMessageReceived : IDisposable
    {
        void Start(Address address, TransactionSettings settings);
        void Stop();
    }
}