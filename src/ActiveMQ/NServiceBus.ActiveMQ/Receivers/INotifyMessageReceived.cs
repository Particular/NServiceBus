namespace NServiceBus.Transport.ActiveMQ.Receivers
{
    using System;

    using NServiceBus.Unicast.Transport.Transactional;

    public interface INotifyMessageReceived : IDisposable
    {
        Action<string, Exception> EndProcessMessage { get; set; }
        Func<TransportMessage, bool> TryProcessMessage { get; set; }
        void Start(Address address, TransactionSettings settings);
        void Stop();
    }
}