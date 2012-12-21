namespace NServiceBus.ActiveMQ
{
    using System;
    using NServiceBus.Unicast.Transport.Transactional;

    public interface INotifyMessageReceived : IDisposable
    {
        Func<TransportMessage, bool> TryProcessMessage { get; set; }
        void Start(Address address, TransactionSettings settings);
    }
}