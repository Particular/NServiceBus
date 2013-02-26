namespace NServiceBus.Transports.ActiveMQ.Receivers.TransactonsScopes
{
    using System;
    using Apache.NMS;

    public interface ITransactionScope : IDisposable
    {
        void MessageAccepted(IMessage message);
        void Complete();
    }
}