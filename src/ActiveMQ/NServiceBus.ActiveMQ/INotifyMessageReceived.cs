namespace NServiceBus.Transport.ActiveMQ
{
    using System;
    using NServiceBus.Unicast.Transport.Transactional;

    public interface INotifyMessageReceived
    {
        Action<string, Exception> EndProcessMessage { get; set; }
        Func<TransportMessage, bool> TryProcessMessage { get; set; }
        void Start(Address address, TransactionSettings settings);
        void Stop();
    }
}